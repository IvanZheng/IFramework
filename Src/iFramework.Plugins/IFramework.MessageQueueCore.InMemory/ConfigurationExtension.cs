

using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.MessageQueue;
using Microsoft.Extensions.DependencyInjection;

namespace IFramework.MessageQueue.InMemory
{
    public static class ConfigurationExtension
    {
        public static Configuration UseInMemoryMessageQueue(this Configuration configuration)
        {
            ObjectProviderFactory.Instance
                      .ObjectProviderBuilder
                      .Register<IMessageQueueClient, InMemoryClient>(ServiceLifetime.Singleton);
            return configuration;
        }
    }
}
