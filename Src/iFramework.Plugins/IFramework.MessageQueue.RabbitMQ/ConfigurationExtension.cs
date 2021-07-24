using System;
using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.Message;
using IFramework.MessageQueue.Client.Abstracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace IFramework.MessageQueue.RabbitMQ
{
    public static class ConfigurationExtension
    {
        public static IServiceCollection AddRabbitMQ(this IServiceCollection services,
                                                ConnectionFactory connectionFactory,
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
            Configuration.Instance.SetCommitPerMessage(true);
            var constructInjection = new ConstructInjection(new ParameterInjection("connectionFactory", connectionFactory));

            services.AddSingleton<IMessageQueueClientProvider, RabbitMQClientProvider>(constructInjection)
                    .AddSingleton<IMessageQueueClient, MessageQueueClient>();
            return services;
        }
    }
}