using System;
using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.MessageQueue;
using IFramework.MessageQueue.Client.Abstracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace IFramework.MessageQueue.ConfluentKafka
{
    public static class ConfigurationExtension
    {
        public static IServiceCollection AddConfluentKafka(this IServiceCollection services,
                                                           Action<KafkaClientOptions> options = null)
        {
            services.AddCustomOptions(options);
            services.AddService<IMessageQueueClientProvider, KafkaMQClientProvider>();
            services.AddService<IMessageQueueClient, MessageQueueClient>(ServiceLifetime.Singleton);
            return services;
        }
    }
}