using IFramework.Config;
using IFramework.Message;
using Microsoft.Extensions.DependencyInjection;

namespace IFramework.MessageStores.Abstracts
{
    public static class ConfigurationExtension
    {
        public static IServiceCollection AddRelationalMessageStore<TMessageStore, TMessageStoreDaemon>(this IServiceCollection services,
                                                                                                       ServiceLifetime lifetime = ServiceLifetime.Scoped)
            where TMessageStore : class, IMessageStore
            where TMessageStoreDaemon: class, IMessageStoreDaemon
        {
            return services.AddMessageStore<TMessageStore>(lifetime)
                           .AddMessageStoreDaemon<TMessageStoreDaemon>();
        }
    }
}