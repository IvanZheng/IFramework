using System;
using System.Linq;
using System.Reflection;
using Autofac;
using IFramework.Config;
using Microsoft.Extensions.DependencyInjection;

namespace IFramework.DependencyInjection.Autofac
{
    public static class ConfigurationExtension
    {
        public static Configuration UseAutofacContainer(this Configuration configuration,
                                                        params string[] assemblies)
        {
            var builder = new ContainerBuilder();
            builder.RegisterAssemblyTypes(assemblies.Select(Assembly.Load).ToArray());
            IoCFactory.Instance.SetProviderBuilder(new ObjectProviderBuilder(builder));
            return configuration;
        }

        public static Configuration UseAutofacContainer(this Configuration configuration,
                                                        ContainerBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }
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