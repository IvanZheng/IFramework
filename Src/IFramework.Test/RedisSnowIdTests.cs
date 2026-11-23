using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IFramework.Infrastructure;
using Xunit;

namespace IFramework.Test
{
    public class RedisSnowIdTests
    {
        private const long Start = 1794976055781;

        [Fact]
        public async Task ConcurrentAllocationAndRedisOutage()
        {
            var store = new Slots();
            long now = Start;
            var workers = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ =>
                Task.Run(() => RedisSnowIdWorker.Acquire(store, null, 0, 0, () => now))));
            Assert.Equal(8, workers.Select(w => w.WorkerId).Distinct().Count());
            store.Offline = true;
            now += 86400000;
            var ids = Enumerable.Range(0, 8000).AsParallel().Select(i => workers[i % 8].NextId()).ToArray();
            Assert.Equal(ids.Length, ids.Distinct().Count());
            Assert.All(ids, id => Assert.Equal(now, (id >> 22) + Snowflake.Core.IdWorker.Twepoch));
            await Assert.ThrowsAsync<IOException>(() => RedisSnowIdWorker.Acquire(store, null, 0, 0, () => now));
            store.Offline = false;
            var ninth = await RedisSnowIdWorker.Acquire(store, null, 0, 0, () => now);
            Assert.DoesNotContain(ninth.WorkerId, workers.Select(w => w.WorkerId));
        }

        [Fact]
        public async Task ReleasedSlotCannotRepeatAnIssuedMillisecond()
        {
            var store = new Slots();
            long now = Start;
            Task<RedisSnowIdWorker> StartWorker() => RedisSnowIdWorker.Acquire(store, 0, 0, 0, () => now);
            var original = await StartWorker();
            var firstId = original.NextId();
            Assert.True(await original.StopAndRelease());
            Assert.Throws<InvalidOperationException>(() => original.NextId());
            await Assert.ThrowsAsync<InvalidOperationException>(() => StartWorker());
            now++;
            var replacement = await StartWorker();
            now--;
            Assert.Throws<InvalidOperationException>(() => replacement.NextId());
            Assert.False(await original.StopAndRelease());
            now++;
            Assert.True(replacement.NextId() > firstId);
        }

        [Fact]
        public async Task LostShutdownKeepsSlotReserved()
        {
            var store = new Slots();
            var worker = await RedisSnowIdWorker.Acquire(store, 3, 2, 0, () => Start);
            store.Offline = true;
            await Assert.ThrowsAsync<IOException>(() => worker.StopAndRelease());
            store.Offline = false;
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                RedisSnowIdWorker.Acquire(store, 3, 2, 0, () => Start + 86400000));
        }

        [Fact]
        public async Task PodBindingSurvivesCleanRestartAndCrash()
        {
            var store = new Slots();
            long now = Start;
            const string pod = "gench-default/paycenter-abc";
            Task<RedisSnowIdWorker> Boot(int first) =>
                RedisSnowIdWorker.AcquirePod(store, null, null, first, () => now, pod);

            var first = await Boot(74);
            long original = first.NextId();
            Assert.True(await first.StopAndRelease());
            var second = await Boot(691);
            Assert.Equal("74", await store.Read("pod:" + pod));
            Assert.Throws<InvalidOperationException>(() => second.NextId());
            now++;
            Assert.True(second.NextId() > original);

            // Simulate process termination without its exit callback running.
            var restarted = await Boot(101);
            Assert.Equal(first.WorkerId, restarted.WorkerId);
            Assert.Equal(first.DatacenterId, restarted.DatacenterId);
            Assert.False(await second.StopAndRelease());
            Assert.Throws<InvalidOperationException>(() => restarted.NextId());
            now++;
            store.Offline = true;
            Assert.True(restarted.NextId() > original);
        }

        [Fact]
        public async Task DifferentPodNamesCompeteForTheSameCandidate()
        {
            var store = new Slots();
            var workers = await Task.WhenAll(Enumerable.Range(0, 8).Select(i =>
                Task.Run(() => RedisSnowIdWorker.AcquirePod(store, null, 0, 0, () => Start, "default/pod-" + i))));
            Assert.Equal(8, workers.Select(w => w.WorkerId).Distinct().Count());
            var process = await RedisSnowIdWorker.Acquire(store, null, 0, 0, () => Start);
            Assert.DoesNotContain(process.WorkerId, workers.Select(w => w.WorkerId));
            Assert.True(await workers[0].StopAndRelease());
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                RedisSnowIdWorker.AcquirePod(store, (int)workers[0].WorkerId, 0, 0, () => Start + 1, "default/new-pod"));
        }

        private sealed class Slots : ISnowIdSlotStore
        {
            private readonly Dictionary<string, string> records = new Dictionary<string, string>();
            public bool Offline;

            public Task<string> Read(string slot)
            {
                lock (records)
                {
                    if (Offline) throw new IOException();
                    records.TryGetValue(slot, out var value);
                    return Task.FromResult(value);
                }
            }

            public Task<bool> Replace(string slot, string expected, string value)
            {
                lock (records)
                {
                    if (Offline) throw new IOException();
                    records.TryGetValue(slot, out var current);
                    if (expected != current) return Task.FromResult(false);
                    records[slot] = value;
                    return Task.FromResult(true);
                }
            }
        }
    }
}
