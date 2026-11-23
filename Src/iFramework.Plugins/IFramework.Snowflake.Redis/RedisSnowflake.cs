using System;
using StackExchange.Redis;

namespace IFramework.Infrastructure
{
    public static class RedisSnowflake
    {
        public static void Configure(IConnectionMultiplexer connection, string prefix = "iframework:snowflake",
                                     bool usePodName = false, string podScope = null)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (string.IsNullOrWhiteSpace(prefix)) throw new ArgumentException("A shared key prefix is required.", nameof(prefix));
            SnowIdWorker.ConfigureFactory(() => RedisSnowIdWorker.Start(connection, prefix, null, null,
                usePodName ? RedisPodIdentity.FromEnvironment(podScope) : null));
        }
    }
}
