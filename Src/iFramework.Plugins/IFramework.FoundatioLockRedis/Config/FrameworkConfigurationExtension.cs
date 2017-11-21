using IFramework.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Foundatio.Caching;
using Foundatio.Lock;
using Foundatio.Messaging;
using IFramework.FoundatioRedis.Caching;
using IFramework.Infrastructure.Caching;
using IFramework.IoC;
using IFramework.MessageQueue;
using StackExchange.Redis;

namespace IFramework.FoundatioRedis.Config
{
    public static class FrameworkConfigurationExtension
    {
        public static Configuration UseFoundatioRedisCache(this Configuration configuration, string redisCacheConnectionString = null, bool preserveAsyncOrder = false)
        {
            ConnectionMultiplexer muxer = GetMuxer(redisCacheConnectionString, preserveAsyncOrder);
            var redisCacheManager = new RedisCacheManager(new RedisCacheClient(muxer));
            IoCFactory.Instance
                      .CurrentContainer
                      .RegisterInstance(typeof(ICacheManager), redisCacheManager);
            return configuration;
        }


        public static Configuration UseFoundatioLockRedis(this Configuration configuration, string redisConnectionString = null, bool preserveAsyncOrder = false)
        {
            ConnectionMultiplexer muxer = GetMuxer(redisConnectionString, preserveAsyncOrder);
            ILockProvider lockerProvider = new CacheLockProvider(new RedisCacheClient(muxer), new RedisMessageBus(muxer.GetSubscriber()));

            IoCFactory.Instance.CurrentContainer
                      .RegisterInstance(typeof(Infrastructure.ILockProvider), new FoundatioLock.LockProvider(lockerProvider));
            return configuration;
        }


        public static ConnectionMultiplexer GetMuxer(string redisConnectionString = null, bool preserveAsyncOrder = false)
        {
            string connectionString = Configuration.GetConnectionString(redisConnectionString ?? "RedisConnectionString");
            if (string.IsNullOrEmpty(connectionString))
                return null;
            var muxer = ConnectionMultiplexer.Connect(connectionString);
            muxer.PreserveAsyncOrder = preserveAsyncOrder;
            return muxer;
        }
    }
}
