using System;
using IFramework.IoC;
using IFramework.Unity;
using Microsoft.Practices.Unity;
using Microsoft.Practices.Unity.Configuration;

namespace IFramework.Config
{
    public static class FrameworkConfigurationExtension
    {
        private static readonly string LifetimeManagerKeyFormat = "IoC.{0}";

        public static string GetLifetimeManagerKey(this Configuration configuration, Lifetime lifetime)
        {
            return string.Format(LifetimeManagerKeyFormat, lifetime);
        }

        /// <summary>
        ///     Use Log4Net as the logger for the enode framework.
        /// </summary>
        /// <returns></returns>
        public static Configuration UseUnityContainer(this Configuration configuration,
                                                      IUnityContainer unityContainer = null)
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

            #region register lifetimemanager

            unityContainer.RegisterType<LifetimeManager, ContainerControlledLifetimeManager>(
                                                                                             configuration.GetLifetimeManagerKey(Lifetime.Singleton));
            unityContainer.RegisterType<LifetimeManager, HierarchicalLifetimeManager>(
                                                                                      configuration.GetLifetimeManagerKey(Lifetime.Hierarchical));
            unityContainer.RegisterType<LifetimeManager, TransientLifetimeManager>(
                                                                                   configuration.GetLifetimeManagerKey(Lifetime.Transient));

            #endregion

            var container = new ObjectContainer(unityContainer);
            IoCFactory.SetContainer(container);
            return configuration;
        }
    }
}