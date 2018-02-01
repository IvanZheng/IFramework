using Autofac;
using IFramework.Config;
using Microsoft.Extensions.DependencyInjection;

namespace IFramework.DependencyInjection.Autofac
{
    public static class ConfigurationExtension
    {
        public static Configuration UseAutofacContainer(this Configuration configuration,
                                                        ContainerBuilder builder = null)
        {
            IoCFactory.Instance.SetProviderBuilder(new ObjectProviderBuilder(builder));
            return configuration;
        }

        public static Configuration UseAutofacContainer(this Configuration configuration,
                                                        IServiceCollection serviceCollection)
        {
            IoCFactory.Instance.SetProviderBuilder(new ObjectProviderBuilder(serviceCollection));
            return configuration;
        }
    }
}