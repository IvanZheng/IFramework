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

namespace IFramework.FoundatioLock.Config
{
    public static class FrameworkConfigurationExtension
    {
        public static Configuration UseFoundatioLockInMemory(this Configuration configuration)
        {
            ILockProvider lockerProvider = new CacheLockProvider(new InMemoryCacheClient(), new InMemoryMessageBus());

            IoCFactory.Instance.CurrentContainer
                      .RegisterInstance(typeof(Infrastructure.ILockProvider), new LockProvider(lockerProvider));
            return configuration;
        }
    }
}
