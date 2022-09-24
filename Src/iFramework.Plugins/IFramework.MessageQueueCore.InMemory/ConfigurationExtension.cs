

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
        public static IServiceCollection AddInMemoryMessageQueue(this IServiceCollection services, bool ensureArrival = false, bool ensureIdempotent = false, bool persistEvent = false)
        {
            services.AddSingleton(new MessageQueueOptions(ensureArrival, ensureIdempotent, persistEvent));

            services.AddService<IMessageQueueClient, InMemoryClient>(ServiceLifetime.Singleton);
            return services;
        }
    }
}
