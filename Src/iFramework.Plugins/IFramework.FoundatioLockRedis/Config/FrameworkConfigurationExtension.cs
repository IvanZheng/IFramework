using IFramework.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Foundatio.Caching;
using Foundatio.Lock;
using Foundatio.Messaging;
using IFramework.IoC;
using IFramework.MessageQueue;
using StackExchange.Redis;

namespace IFramework.FoundatioLockRedis.Config
{
    public static class FrameworkConfigurationExtension
    {
        public static Configuration UseFoundatioLockRedis(this Configuration configuration, string redisConnectionString = null)
        {
            ConnectionMultiplexer muxer = GetMuxer(redisConnectionString);
            ILockProvider lockerProvider = new CacheLockProvider(new RedisCacheClient(muxer), new RedisMessageBus(muxer.GetSubscriber()));

            IoCFactory.Instance.CurrentContainer
                      .RegisterInstance(typeof(Infrastructure.ILockProvider), new FoundatioLock.LockProvider(lockerProvider));
            return configuration;
        }

        private static ConnectionMultiplexer _muxer;

        public static ConnectionMultiplexer GetMuxer(string redisConnectionString = null)
        {
            string connectionString = Configuration.GetConnectionString(redisConnectionString ?? "RedisConnectionString");
            if (String.IsNullOrEmpty(connectionString))
                return null;

            if (_muxer == null)
            {
                _muxer = ConnectionMultiplexer.Connect(connectionString);
                _muxer.PreserveAsyncOrder = false;
            }

            return _muxer;
        }
    }
}
