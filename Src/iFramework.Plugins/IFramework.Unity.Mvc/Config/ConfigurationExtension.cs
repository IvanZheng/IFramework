using IFramework.IoC;
using Microsoft.Practices.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.Config
{
    public static class ConfigurationExtension
    {
        public static Configuration UseUnityMvc(this Configuration configuration)
        {
            IoCFactory.Instance
                      .CurrentContainer
                      .RegisterType<LifetimeManager, PerRequestLifetimeManager>(
                        configuration.GetLifetimeManagerKey(Lifetime.PerRequest)
                );
            return configuration;
        }
    }
}
