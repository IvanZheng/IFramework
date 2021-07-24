﻿

using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.Message;
using IFramework.MessageQueue;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace IFramework.MessageQueue.InMemory
{
    public static class ConfigurationExtension
    {
        public static IServiceCollection AddInMemoryMessageQueue(this IServiceCollection services)
        {
            services.AddCustomOptions<MessageQueueOptions>(options =>
            {
                options.EnableIdempotent = false;
                options.EnsureArrival = false;
                options.PersistEvent = false;
            });
            services.AddService<IMessageQueueClient, InMemoryClient>(ServiceLifetime.Singleton);
            return services;
        }
    }
}
