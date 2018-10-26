using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.MessageQueue.Client.Abstracts;
using Microsoft.Extensions.DependencyInjection;

namespace IFramework.MessageQueue.RabbitMQ
{
    public static class ConfigurationExtension
    {
        public static Configuration UseRabbitMQ(this Configuration configuration,
                                                string hostName,
                                                int port = 5672)
        {
            configuration.SetCommitPerMessage(true);
            var constructInjection = new ConstructInjection(new ParameterInjection("hostName", hostName),
                                                            new ParameterInjection("port", port));
            ObjectProviderFactory.Instance
                                 .ObjectProviderBuilder
                                 .Register<IMessageQueueClientProvider, RabbitMQClientProvider>(ServiceLifetime.Singleton,
                                                                                                constructInjection)
                                 .Register<IMessageQueueClient, MessageQueueClient>(ServiceLifetime.Singleton);
            return configuration;
        }
    }
}