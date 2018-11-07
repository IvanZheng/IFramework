using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.MessageQueue.Client.Abstracts;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace IFramework.MessageQueue.RabbitMQ
{
    public static class ConfigurationExtension
    {
        public static Configuration UseRabbitMQ(this Configuration configuration,
                                                ConnectionFactory connectionFactory)
        {
            configuration.SetCommitPerMessage(true);
            var constructInjection = new ConstructInjection(new ParameterInjection("connectionFactory", connectionFactory));

            ObjectProviderFactory.Instance
                                 .ObjectProviderBuilder
                                 .Register<IMessageQueueClientProvider, RabbitMQClientProvider>(ServiceLifetime.Singleton,
                                                                                                constructInjection)
                                 .Register<IMessageQueueClient, MessageQueueClient>(ServiceLifetime.Singleton);
            return configuration;
        }
    }
}