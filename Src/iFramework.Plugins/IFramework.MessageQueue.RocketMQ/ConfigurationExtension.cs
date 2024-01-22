using System;
using IFramework.DependencyInjection;
using IFramework.Message;
using IFramework.MessageQueue.Client.Abstracts;
using Microsoft.Extensions.DependencyInjection;

namespace IFramework.MessageQueue.RocketMQ
{
    public static class ConfigurationExtension
    {
        public static IServiceCollection AddRocketMQ(this IServiceCollection services,
                                                     MessageQueueOptions mqOptions = null,
                                                     Action<RocketMQClientOptions> options = null)
        {
            services.AddSingleton(mqOptions ?? new MessageQueueOptions());
            services.AddCustomOptions(options);
            services.AddService<IMessageQueueClientProvider, RocketMQClientProvider>();
            services.AddService<IMessageQueueClient, MessageQueueClient>(ServiceLifetime.Singleton);
            return services;
        }
    }
}