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
                                                           Action<KafkaClientOptions> options = null,
                                                           Action<MessageQueueOptions> mqOptions = null)
        {
            if (mqOptions == null)
            {
                services.AddCustomOptions<MessageQueueOptions>(o =>
                {
                    o.EnableIdempotent = true;
                    o.EnsureArrival = true;
                    o.PersistEvent = true;
                });
            }
            else
            {
                services.AddCustomOptions(mqOptions);
            }
            services.AddCustomOptions(options);
            services.AddService<IMessageQueueClientProvider, KafkaMQClientProvider>();
            services.AddService<IMessageQueueClient, MessageQueueClient>(ServiceLifetime.Singleton);
            return services;
        }
    }
}