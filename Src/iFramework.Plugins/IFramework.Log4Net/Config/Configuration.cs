using IFramework.Infrastructure.Logging;
using IFramework.IoC;
using IFramework.Log4Net;

namespace IFramework.Config
{
    public static class IFrameworkConfigurationExtension
    {
        /// <summary>
        ///     Use Log4Net as the logger for the enode framework.
        /// </summary>
        /// <returns></returns>
        public static Configuration UseLog4Net(this Configuration configuration, string configFile = "log4net.config")
        {
            IoCFactory.Instance.CurrentContainer
                      .RegisterInstance(typeof(ILoggerFactory)
                                        , new Log4NetLoggerFactory(configFile));
            return configuration;
        }
    }
}