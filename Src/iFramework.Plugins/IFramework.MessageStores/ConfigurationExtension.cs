using IFramework.Config;
using IFramework.Message;
using Microsoft.Extensions.DependencyInjection;

namespace IFramework.MessageStores.Relational
{
    public static class ConfigurationExtension
    {
        internal static  bool UseInMemoryDatabase { get; set; }
        public static Configuration UseRelationalMessageStore<TMessageStore>(this Configuration configuration,
                                                                             bool useInMemoryDatabase = false,
                                                                             ServiceLifetime lifetime = ServiceLifetime.Scoped)
            where TMessageStore : class, IMessageStore
        {
            UseInMemoryDatabase = useInMemoryDatabase;
            return configuration.UseMessageStore<TMessageStore>(lifetime)
                                .UseMessageStoreDaemon<MessageStoreDaemon>();
        }
    }
}