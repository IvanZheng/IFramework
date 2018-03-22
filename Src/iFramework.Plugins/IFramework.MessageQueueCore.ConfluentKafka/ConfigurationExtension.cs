using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.MessageQueue;
using IFramework.MessageQueue.Client.Abstracts;
using Microsoft.Extensions.DependencyInjection;

namespace IFramework.MessageQueueCore.ConfluentKafka
{
    public static class ConfigurationExtension
    {
        public static Configuration UseConfluentKafka(this Configuration configuration,
                                                      string brokerList)
        {
            IoCFactory.Instance
                      .ObjectProviderBuilder
                      .Register<IMessageQueueClientProvider, KafkaMQClientProvider>(ServiceLifetime.Singleton,
                                                                                              new ConstructInjection(new ParameterInjection("brokerList", brokerList)))
                      .Register<IMessageQueueClient, MessageQueueClient>(ServiceLifetime.Singleton);
            return configuration;
        }
    }
}