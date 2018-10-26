using IFramework.Config;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using Unity;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Unity.Lifetime;

namespace IFramework.DependencyInjection.Unity
{
    public static class ConfigurationExtension
    {
        public static Configuration UseUnityContainer(this Configuration configuration, UnityContainer container = null)
        {
            container = container ?? new UnityContainer();

            ObjectProviderFactory.Instance.SetProviderBuilder(new ObjectProviderBuilder(container));
            return configuration;
        }
    }
}
