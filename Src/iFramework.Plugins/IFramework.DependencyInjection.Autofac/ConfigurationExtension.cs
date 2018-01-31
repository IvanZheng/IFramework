using System;
using System.Collections.Generic;
using System.Text;
using Autofac;
using IFramework.Config;

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
    }
}
