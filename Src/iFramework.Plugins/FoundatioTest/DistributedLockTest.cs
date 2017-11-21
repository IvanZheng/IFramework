
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
    public class DistributedLockTest
    {
        //static readonly ConnectionMultiplexer Muxer = SharedConnection.GetMuxer();
        //static readonly ILockProvider Locker = new CacheLockProvider(new RedisCacheClient(Muxer), new RedisMessageBus(Muxer.GetSubscriber()));
        //static ILockProvider Locker = new CacheLockProvider(new InMemoryCacheClient(), new InMemoryMessageBus());
        private ILockProvider _lockProvider;

        private ICacheManager _cacheManager;
        private static int _sum = 0;

        [TestInitialize]
        public void Initialize()
        {
            Configuration.Instance
                         .UseUnityContainer()
                         .RegisterCommonComponents()
                         .UseFoundatioRedisCache()
                         .UseFoundatioLockRedis();
                        // .UseFoundatioLockInMemory();

            _lockProvider = IoCFactory.Resolve<ILockProvider>();
            _cacheManager = IoCFactory.Resolve<ICacheManager>();
        }


        [TestMethod]
        public void TestDistributedLock()
        {
            var tasks = new List<Task>();
            var n = 10000;
            for (int i = 0; i < n; i++)
            {
                tasks.Add(AddSum());
            }
            Console.WriteLine($"{DateTime.Now} start waiting");
            Task.WaitAll(tasks.ToArray());
            Console.WriteLine($"{DateTime.Now} end waiting sum:{_sum}");
            Assert.IsTrue(_sum == n);
        }

        private Task AddSum()
        {
            return _lockProvider.LockAsync("test", () => _sum ++, TimeSpan.FromSeconds(10));
        }

        [TestMethod]
        public async Task TestDistributedLockAsync()
        {
            var tasks = new List<Task>();
            var n = 10000;
            for (int i = 0; i < n; i++)
            {
                tasks.Add(AddSumAsync());
            }
            Console.WriteLine($"{DateTime.Now} start waiting");
            await Task.WhenAll(tasks.ToArray());
            Console.WriteLine($"{DateTime.Now} end waiting sum:{_sum}");
            Assert.IsTrue(_sum == n);
        }

        private Task AddSumAsync()
        {
            return _lockProvider.LockAsync("test", DoAsync, TimeSpan.FromSeconds(10));
        }

        private Task DoAsync()
        {
            return Task.Run(() => _sum++);
        }


        [TestMethod]
        public async Task TestAll()
        {
            await TestRedisCacheAsync();
            await TestDistributedLockAsync();
        }

        [TestMethod]
        public async Task TestRedisCacheAsync()
        {
            await _cacheManager.ClearAsync().ConfigureAwait(false);
            int t = 0;
            var result = await _cacheManager.GetAsync<int>("key");
            Assert.IsTrue(!result.HasValue);
            result = await _cacheManager.GetAsync("key", 1, async () => await Task.FromResult(++t))
                                        .ConfigureAwait(false);
            var tasks = new List<Task>();

            for (int i = 0; i < 100; i++)
            {
                tasks.Add(_cacheManager.GetAsync("key", 1, async () => await Task.FromResult(++t)));
            }

            await Task.WhenAll(tasks.ToArray());

            result = await _cacheManager.GetAsync("key", 1, async () => await Task.FromResult(++t))
                                            .ConfigureAwait(false);
            Assert.IsTrue(result.HasValue && result.Value == 1 && t == 1);
        }
    }
}
