using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Foundatio.Caching;
using Foundatio.Lock;
using Foundatio.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StackExchange.Redis;

namespace IFramework4._5Tests
{
    [TestClass]
    public class DistributedLockTest
    {
        static readonly ConnectionMultiplexer Muxer = SharedConnection.GetMuxer();
        static readonly ILockProvider Locker = new CacheLockProvider(new RedisCacheClient(Muxer), new RedisMessageBus(Muxer.GetSubscriber()));
        private static int _sum = 0;

        [TestMethod]
        public async Task TestDistributedLock()
        {
            //ILockProvider locker = new CacheLockProvider(new InMemoryCacheClient(), new InMemoryMessageBus());
            var tasks = new List<Task>();
            var n = 10;
            for (int i = 0; i < n; i++)
            {
                var i1 = i;
                tasks.Add(AddSum(i1));
            }
            Console.WriteLine($"{DateTime.Now} start waiting");
            await Task.WhenAll(tasks.ToArray());
            Console.WriteLine($"{DateTime.Now} start waiting");
            Assert.IsTrue(_sum == n);
        }

        private async Task AddSum(int i)
        {
            var @lock = await Locker.AcquireAsync("test", TimeSpan.FromSeconds(10))
                                    .ConfigureAwait(false);
            try
            {
                Console.WriteLine($"{DateTime.Now} {i}: {++_sum}");
            }
            finally
            {
                await @lock.ReleaseAsync()
                           .ConfigureAwait(false);
            }
          
        }
    }
}
