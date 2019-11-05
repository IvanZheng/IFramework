using IFramework.Config;
using IFramework.Message;
using IFramework.MessageStores.Abstracts;
using Microsoft.Extensions.DependencyInjection;

namespace IFramework.MessageStores.Relational
{
    public static class ConfigurationExtension
    {
        public static IServiceCollection AddRelationalMessageStore<TMessageStore>(this IServiceCollection services,
                                                                                  ServiceLifetime lifetime = ServiceLifetime.Scoped)
            where TMessageStore : class, IMessageStore
        {
            return services.AddRelationalMessageStore<TMessageStore, MessageStoreDaemon>(lifetime);
        }
    }
}