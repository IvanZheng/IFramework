using System;
using System.Linq;
using System.Reflection;
using IFramework.Config;
using Microsoft.Extensions.DependencyInjection;

namespace IFramework.DependencyInjection.Microsoft
{
    public static class ConfigurationExtension
    {
        public static Configuration UseMicrosoftDependencyInjection(this Configuration configuration)
        {
            var serviceCollection = new ServiceCollection();
            ObjectProviderFactory.Instance.SetProviderBuilder(new ObjectProviderBuilder(serviceCollection));
            return configuration;
        }

        public static Configuration UseAutofacContainer(this Configuration configuration,
                                                        IServiceCollection serviceCollection)
        {
            ObjectProviderFactory.Instance.SetProviderBuilder(new ObjectProviderBuilder(serviceCollection));
            return configuration;
        }
    }
}