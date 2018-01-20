
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IFramework.Config;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Caching;
using IFramework.IoC;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IFramework.FoundatioRedis.Config;

namespace IFramework4._5Tests
{
    [TestClass]
    public class CacheTests
    {
        //static readonly ConnectionMultiplexer Muxer = SharedConnection.GetMuxer();
        //static readonly ILockProvider Locker = new CacheLockProvider(new RedisCacheClient(Muxer), new RedisMessageBus(Muxer.GetSubscriber()));
        //static ILockProvider Locker = new CacheLockProvider(new InMemoryCacheClient(), new InMemoryMessageBus());
    

        private ICacheManager _cacheManager;
        private static int _sum = 0;

        [TestInitialize]
        public void Initialize()
        {
            Configuration.Instance
                         .UseUnityContainer()
                         .RegisterCommonComponents();

            _cacheManager = IoCFactory.Resolve<ICacheManager>();
        }

        public class A
        {
            
        }
        private int BaseValue = 0;
        [TestMethod]
        public async Task TestCache()
        {
            var key = "key";
            var cache = await _cacheManager.GetAsync<A>(key);
            var value = cache.Value;
            Assert.IsFalse(cache.HasValue);
            Assert.IsNull(value);


            cache = await _cacheManager.GetAsync<A>(key, () => GetValue());
            value = cache.Value;
            Assert.IsTrue(cache.HasValue);
            Assert.IsNull(value);

        }

        private A GetValue()
        {
            return null;
        }
      
    }
}
