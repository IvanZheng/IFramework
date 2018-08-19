using System;
using System.Collections.Generic;
using System.Text;
using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.MessageQueue.Client.Abstracts;
using Microsoft.Extensions.DependencyInjection;

namespace IFramework.MessageQueue.RabbitMQ
{
    public static class ConfigurationExtension
    {
        public static Configuration UseRabbitMQ(this Configuration configuration,
                                                      string hostName)
        {
            configuration.SetCommitPerMessage(true);
            ObjectProviderFactory.Instance
                                 .ObjectProviderBuilder
                                 .Register<IMessageQueueClientProvider, RabbitMQClientProvider>(ServiceLifetime.Singleton,
                                                                                               new ConstructInjection(new ParameterInjection("hostName", hostName)))
                                 .Register<IMessageQueueClient, MessageQueueClient>(ServiceLifetime.Singleton);
            return configuration;
        }
    }
}
