

using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.MessageQueue;
using Microsoft.Extensions.DependencyInjection;

namespace IFramework.MessageQueue.InMemory
{
    public static class ConfigurationExtension
    {
        public static IServiceCollection AddInMemoryMessageQueue(this IServiceCollection services)
        {
            services.RegisterType<IMessageQueueClient, InMemoryClient>(ServiceLifetime.Singleton);
            return services;
        }
    }
}
