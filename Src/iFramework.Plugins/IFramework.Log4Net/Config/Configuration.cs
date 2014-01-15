using IFramework.Config;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Logging;
using IFramework.Log4Net;
using Microsoft.Practices.Unity;


namespace IFramework.Config
{
    public static class IFrameworkConfigurationExtension
    {
        /// <summary>Use Log4Net as the logger for the enode framework.
        /// </summary>
        /// <returns></returns>
        public static Configuration UseLog4Net(this Configuration configuration)
        {
            return UseLog4Net(configuration, "log4net.config");
        }
        /// <summary>Use Log4Net as the logger for the enode framework.
        /// </summary>
        /// <returns></returns>
        public static Configuration UseLog4Net(this Configuration configuration, string configFile)
        {
             IoCFactory.Instance.CurrentContainer
                                .RegisterInstance(typeof(ILoggerFactory)
                                           , new Log4NetLoggerFactory(configFile)
                                           , new ContainerControlledLifetimeManager());
            return configuration;
        }
    }
}
