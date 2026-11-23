using System;
using StackExchange.Redis;
using IFramework.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IFramework.Infrastructure
{
    public static class RedisSnowflake
    {
        public static void Configure(IConnectionMultiplexer connection, string prefix = "iframework:snowflake",
                                     bool usePodName = false, string podScope = null)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (string.IsNullOrWhiteSpace(prefix)) throw new ArgumentException("A shared key prefix is required.", nameof(prefix));
            SnowIdWorker.ConfigureFactory(() =>
            {
                var factory = ObjectProviderFactory.Instance.TryGetObjectProvider()?.GetService<ILoggerFactory>();
                var logger = factory?.CreateLogger($"{podScope ?? "IFramework.Infrastructure"}.RedisSnowflake");
                string identity = null;
                try
                {
                    identity = usePodName ? RedisPodIdentity.FromEnvironment(podScope) : null;
                    var worker = RedisSnowIdWorker.Start(connection, prefix, null, null, identity);
                    logger?.LogInformation(
                        "RedisSnowflake initialized. Machine={MachineName} UsePodName={UsePodName} PodIdentity={PodIdentity} Prefix={RedisKeyPrefix} Database={RedisDatabase} Slot={Slot} WorkerId={WorkerId} DatacenterId={DatacenterId} RetentionHours={RetentionHours} RefreshMinutes={RefreshMinutes}",
                        Environment.MachineName, usePodName, identity, prefix, connection.GetDatabase().Database,
                        (worker.DatacenterId << 5) | worker.WorkerId, worker.WorkerId, worker.DatacenterId,
                        RedisSnowIdSlotStore.Retention.TotalHours, RedisSnowIdWorker.RefreshInterval.TotalMinutes);
                    if (logger == null)
                        Console.WriteLine($"RedisSnowflake initialized. Machine={Environment.MachineName} UsePodName={usePodName} PodIdentity={identity} Prefix={prefix} Database={connection.GetDatabase().Database} Slot={(worker.DatacenterId << 5) | worker.WorkerId} WorkerId={worker.WorkerId} DatacenterId={worker.DatacenterId} RetentionHours={RedisSnowIdSlotStore.Retention.TotalHours} RefreshMinutes={RedisSnowIdWorker.RefreshInterval.TotalMinutes}");
                    return worker;
                }
                catch (Exception ex)
                {
                    logger?.LogError("RedisSnowflake initialization failed. Machine={MachineName} UsePodName={UsePodName} PodScope={PodScope} PodIdentity={PodIdentity} Prefix={RedisKeyPrefix} ErrorType={ErrorType}",
                        Environment.MachineName, usePodName, podScope, identity, prefix, ex.GetType().Name);
                    if (logger == null) Console.Error.WriteLine($"RedisSnowflake initialization failed. PodIdentity={identity} Prefix={prefix} ErrorType={ex.GetType().Name}");
                    throw;
                }
            });
        }
    }
}
