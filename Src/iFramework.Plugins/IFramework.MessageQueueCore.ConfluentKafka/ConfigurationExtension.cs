using System;
using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.Message;
using IFramework.MessageQueue;
using IFramework.MessageQueue.Client.Abstracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace IFramework.MessageQueue.ConfluentKafka
{
    public static class ConfigurationExtension
    {
        
        public static IServiceCollection AddConfluentKafka(this IServiceCollection services,
                                                           MessageQueueOptions mqOptions = null,
                                                           Action<KafkaClientOptions> options = null)
        {
            services.AddSingleton(mqOptions ?? new MessageQueueOptions());
            services.AddCustomOptions(options);
            services.AddService<IMessageQueueClientProvider, KafkaMQClientProvider>();
            services.AddService<IMessageQueueClient, MessageQueueClient>(ServiceLifetime.Singleton);
            services.AddSingleton<KafkaManager>();
            return services;
        }
    }
}