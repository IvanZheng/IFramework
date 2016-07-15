using IFramework.Config;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Logging;
using IFramework.IoC;
using IFramework.Unity;
using Microsoft.Practices.Unity;

namespace IFramework.Config
{
    public static class IFrameworkConfigurationExtension
    {
        /// <summary>Use Log4Net as the logger for the enode framework.
        /// </summary>
        /// <returns></returns>
        public static Configuration UseUnityContainer(this Configuration configuration, IUnityContainer unityContainer = null)
        {
            IoCFactory.SetContainer(new ObjectContainer(unityContainer));
            return configuration;
        }
    }
}
