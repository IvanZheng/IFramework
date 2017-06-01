using IFramework.IoC;
using Microsoft.Practices.Unity;

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