using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IFramework.Config;
using IFramework.FoundatioLockRedis.Config;
using IFramework.Infrastructure;
using IFramework.IoC;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IFramework.FoundatioLock.Config;

namespace IFramework4._5Tests
{
    [TestClass]
    public class DistributedLockTest
    {
        //static readonly ConnectionMultiplexer Muxer = SharedConnection.GetMuxer();
        //static readonly ILockProvider Locker = new CacheLockProvider(new RedisCacheClient(Muxer), new RedisMessageBus(Muxer.GetSubscriber()));
        //static ILockProvider Locker = new CacheLockProvider(new InMemoryCacheClient(), new InMemoryMessageBus());
        private static ILockProvider _lockerProvider;
        private static int _sum = 0;

        [TestInitialize]
        public void Initialize()
        {
            Configuration.Instance
                         .UseUnityContainer()
                         .UseFoundatioLockRedis();
                        // .UseFoundatioLockInMemory();

            _lockerProvider = IoCFactory.Resolve<ILockProvider>();
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
            return _lockerProvider.LockAsync("test", () => _sum ++, TimeSpan.FromSeconds(10));
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
            return _lockerProvider.LockAsync("test", DoAsync, TimeSpan.FromSeconds(10));
        }

        private Task DoAsync()
        {
            return Task.Run(() => _sum++);
        }
    }
}
