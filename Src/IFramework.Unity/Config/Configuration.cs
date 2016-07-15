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
        /// <summary>Use Log4Net as the logger for the enode framework.
        /// </summary>
        /// <returns></returns>
        public static Configuration UseUnityContainer(this Configuration configuration, IUnityContainer unityContainer = null)
        {
            if (unityContainer == null)
            {
                unityContainer = new UnityContainer();
                try
                {
                    unityContainer.LoadConfiguration();
                }
                catch (Exception)
                {
                }
            }
            IoCFactory.SetContainer(new ObjectContainer(unityContainer));
            return configuration;
        }
    }
}
