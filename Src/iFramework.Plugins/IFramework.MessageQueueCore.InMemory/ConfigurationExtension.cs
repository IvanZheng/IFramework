

using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.MessageQueue;
using Microsoft.Extensions.DependencyInjection;

namespace IFramework.MessageQueueCore.InMemory
{
    public static class ConfigurationExtension
    {
        public static Configuration UseInMemoryMessageQueue(this Configuration configuration)
        {
            IoCFactory.Instance
                      .ObjectProviderBuilder
                      .Register<IMessageQueueClient, InMemoryClient>(ServiceLifetime.Singleton);
            return configuration;
        }
    }
}
