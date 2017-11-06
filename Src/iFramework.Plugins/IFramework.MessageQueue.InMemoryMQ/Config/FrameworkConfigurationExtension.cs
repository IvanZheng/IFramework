using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IFramework.Config;
using IFramework.Event;
using IFramework.Event.Impl;
using IFramework.IoC;
using IFramework.MessageQueue;

namespace IFramework.MessageQueue.InMemoryMQ
{
    public static class FrameworkConfigurationExtension
    {
        public static Configuration UseInMemoryMessageQueue(this Configuration configuration)
        {
            IoCFactory.Instance.CurrentContainer
                      .RegisterType<IMessageQueueClient, InMemoryClient>(Lifetime.Singleton);
            return configuration;
        }
    }
}
