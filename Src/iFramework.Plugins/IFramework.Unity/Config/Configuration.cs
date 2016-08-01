using IFramework.Config;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Logging;
using IFramework.IoC;
using IFramework.Unity;
using Microsoft.Practices.Unity;
using Microsoft.Practices.Unity.Configuration;
using System;

namespace IFramework.Config
{
    public static class IFrameworkConfigurationExtension
    {
        static readonly string LifetimeManagerKeyFormat = "IoC.{0}";
        public static string GetLifetimeManagerKey(this Configuration configuration, Lifetime lifetime)
        {
            return string.Format(LifetimeManagerKeyFormat, lifetime);
        }
        /// <summary>Use Log4Net as the logger for the enode framework.
        /// </summary>
        /// <returns></returns>
        public static Configuration UseUnityContainer(this Configuration configuration, IUnityContainer unityContainer = null)
        {
            if (IoCFactory.IsInit())
            {
                return configuration;
            }
            if (unityContainer == null)
            {
                unityContainer = new UnityContainer();
                try
                {
                    unityContainer.LoadConfiguration();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.GetBaseException().Message);
                }
            }
            var container = new ObjectContainer(unityContainer);

            #region register lifetimemanager
            container.RegisterType<LifetimeManager, ContainerControlledLifetimeManager>(configuration.GetLifetimeManagerKey(Lifetime.Singleton));
            container.RegisterType<LifetimeManager, HierarchicalLifetimeManager>(configuration.GetLifetimeManagerKey(Lifetime.Hierarchical));
            container.RegisterType<LifetimeManager, TransientLifetimeManager>(configuration.GetLifetimeManagerKey(Lifetime.Transient));
            #endregion

            IoCFactory.SetContainer(container);
            return configuration;
        }
    }
}
