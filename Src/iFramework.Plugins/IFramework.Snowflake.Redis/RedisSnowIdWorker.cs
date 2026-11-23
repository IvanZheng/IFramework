using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Snowflake.Core;
using StackExchange.Redis;

[assembly: InternalsVisibleTo("IFramework.Test")]

namespace IFramework.Infrastructure
{
    internal sealed class RedisSnowIdWorker : IdWorker
    {
        internal static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(10);
        private const int SlotCount = 1024;
        private const string Free = "free:";
        private readonly ISnowIdSlotStore store;
        private readonly string slotKey;
        private readonly string releasePrefix;
        private readonly string owner;
        private readonly Func<long> clock;
        private readonly object gate = new object();
        private readonly long previousTimestamp;
        private long lastTimestamp;
        private bool stopped;

        private RedisSnowIdWorker(ISnowIdSlotStore store, int slot, string owner, long previousTimestamp,
                                  Func<long> clock, string releasePrefix = Free) : base(slot & 31, slot >> 5)
        {
            this.store = store;
            this.slotKey = slot.ToString(CultureInfo.InvariantCulture);
            this.releasePrefix = releasePrefix;
            this.owner = owner;
            this.previousTimestamp = previousTimestamp;
            this.clock = clock;
            lastTimestamp = previousTimestamp;
        }

        internal static IdWorker Start(IConnectionMultiplexer redis, string prefix, int? worker, int? datacenter, string pod)
        {
            var store = new RedisSnowIdSlotStore(redis.GetDatabase(), prefix ?? "iframework:snowflake");
            var first = Guid.NewGuid().GetHashCode() & 1023;
            Func<long> clock = () => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var instance = (pod == null ? Acquire(store, worker, datacenter, first, clock) :
                AcquirePod(store, worker, datacenter, first, clock, pod)).GetAwaiter().GetResult();
            _ = instance.RefreshWhileRunning(pod);
            AppDomain.CurrentDomain.ProcessExit += (_, __) =>
            {
                // If shutdown times out the slot stays occupied. It never expires under a live issuer.
                try { instance.StopAndRelease().Wait(TimeSpan.FromSeconds(2)); }
                catch (Exception ex) { Trace.TraceWarning("Snowflake Redis slot release failed: {0}", ex.GetBaseException().Message); }
            };
            return instance;
        }

        internal static async Task<RedisSnowIdWorker> Acquire(ISnowIdSlotStore store, int? worker,
                                                               int? datacenter, int first, Func<long> clock)
        {
            // Unique per process incarnation, even for multiple processes with the same hostname.
            var owner = "held:" + Guid.NewGuid().ToString("N") + ":" + Environment.MachineName;
            for (int offset = 0; offset < SlotCount; offset++)
            {
                int slot = (first + offset) % SlotCount;
                if (worker.HasValue && (slot & 31) != worker.Value) continue;
                if (datacenter.HasValue && (slot >> 5) != datacenter.Value) continue;

                var existing = await store.Read(slot.ToString(CultureInfo.InvariantCulture)).ConfigureAwait(false);
                long previous = -1;
                if (existing != null &&
                    (!existing.StartsWith(Free, StringComparison.Ordinal) ||
                     !long.TryParse(existing.Substring(Free.Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out previous) ||
                     previous < -1 || clock() <= previous)) continue;

                if (await store.Replace(slot.ToString(CultureInfo.InvariantCulture), existing, owner).ConfigureAwait(false))
                    return new RedisSnowIdWorker(store, slot, owner, previous, clock);
            }
            throw new InvalidOperationException("No free Snowflake Redis slot in the configured node range.");
        }

        internal static async Task<RedisSnowIdWorker> AcquirePod(ISnowIdSlotStore store, int? worker,
            int? datacenter, int first, Func<long> clock, string pod)
        {
            string index = "pod:" + pod;
            string running = "running:" + pod + ":";
            string parked = "parked:" + pod + ":";
            string incarnation = Guid.NewGuid().ToString("N");
            string pointer = await store.Read(index).ConfigureAwait(false);

            if (pointer != null)
            {
                if (!int.TryParse(pointer, NumberStyles.None, CultureInfo.InvariantCulture, out first) ||
                    first >= SlotCount || (worker.HasValue && (first & 31) != worker.Value) ||
                    (datacenter.HasValue && (first >> 5) != datacenter.Value))
                    throw new InvalidOperationException("Invalid or incompatible Snowflake Pod binding: " + index);
            }

            for (int offset = 0; offset < SlotCount; offset++)
            {
                int slot = (first + offset) % SlotCount;
                if (worker.HasValue && (slot & 31) != worker.Value) continue;
                if (datacenter.HasValue && (slot >> 5) != datacenter.Value) continue;
                string key = slot.ToString(CultureInfo.InvariantCulture);
                string value = await store.Read(key).ConfigureAwait(false);
                bool crashed = value != null && value.StartsWith(running, StringComparison.Ordinal);
                long floor = -1;
                if (value != null)
                {
                    if (!crashed && !value.StartsWith(parked, StringComparison.Ordinal) &&
                        !value.StartsWith(Free, StringComparison.Ordinal)) continue;
                    if (!long.TryParse(value.Substring(value.LastIndexOf(':') + 1), NumberStyles.Integer,
                        CultureInfo.InvariantCulture, out floor) || floor < -1)
                        throw new InvalidOperationException("Invalid Snowflake slot timestamp: " + key);
                }

                // Publish the lookup before claiming the slot. A crash between these writes
                // leaves an unclaimed candidate. The per-slot CAS is the ownership decision.
                if (pointer != key)
                {
                    if (!await store.Replace(index, pointer, key).ConfigureAwait(false))
                        throw new InvalidOperationException("Concurrent initialization for Snowflake Pod " + pod);
                    pointer = key;
                }
                if (crashed) floor = Math.Max(floor, clock());
                string owner = running + incarnation + ":" + floor.ToString(CultureInfo.InvariantCulture);
                if (!await store.Replace(key, value, owner).ConfigureAwait(false))
                {
                    if (crashed || (value != null && value.StartsWith(parked, StringComparison.Ordinal)))
                        throw new InvalidOperationException("Concurrent takeover for Snowflake Pod " + pod);
                    continue;
                }
                if (crashed && clock() == floor) await Task.Delay(2).ConfigureAwait(false);
                return new RedisSnowIdWorker(store, slot, owner, floor, clock, parked);
            }
            throw new InvalidOperationException("No Snowflake slot available for Pod " + pod);
        }

        private async Task RefreshWhileRunning(string pod)
        {
            while (true)
            {
                await Task.Delay(RefreshInterval).ConfigureAwait(false);
                lock (gate) { if (stopped) return; }
                try
                {
                    if (!await store.Replace(slotKey, owner, owner).ConfigureAwait(false))
                    {
                        lock (gate) { stopped = true; }
                        Trace.TraceError("Snowflake slot {0} has been reclaimed. Restart this issuer.", slotKey);
                        return;
                    }
                    if (pod != null)
                        await store.Replace("pod:" + pod, slotKey, slotKey).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("Snowflake Redis heartbeat failed: {0}", ex.Message);
                }
            }
        }
        protected override long TimeGen()
        {
            long now = clock();
            if (now <= previousTimestamp)
                throw new InvalidOperationException("Snowflake clock has not passed the previous owner's last timestamp.");
            return now;
        }

        public override long NextId()
        {
            lock (gate)
            {
                if (stopped) throw new InvalidOperationException("Snowflake generator has stopped.");
                long id = base.NextId();
                lastTimestamp = (id >> 22) + Twepoch;
                return id;
            }
        }

        internal Task<bool> StopAndRelease()
        {
            lock (gate)
            {
                stopped = true;
                // Keep the timestamp across handover, even if this process never issued an ID.
                return store.Replace(slotKey, owner, releasePrefix + lastTimestamp.ToString(CultureInfo.InvariantCulture));
            }
        }
    }
}
