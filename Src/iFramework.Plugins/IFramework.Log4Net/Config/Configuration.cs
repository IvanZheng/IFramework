using System;
using IFramework.Infrastructure.Logging;
using IFramework.IoC;
using IFramework.Log4Net;

namespace IFramework.Config
{
    public static class Log4NetConfiguration
    {
        /// <summary>
        ///     Use Log4Net as the logger for the enode framework.
        /// </summary>
        /// <returns></returns>
        public static Configuration UseLog4Net(this Configuration configuration, 
                                               string defaultApp, 
                                               Level defaultLevel = Level.Debug, 
                                               string configFile = "log4net.config")
        {
            if (string.IsNullOrWhiteSpace(defaultApp))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(defaultApp));
            }
            var loggerLevelController = IoCFactory.Resolve<ILoggerLevelController>();
            IoCFactory.Instance.CurrentContainer
                      .RegisterInstance(typeof(ILoggerFactory)
                                        , new Log4NetLoggerFactory(configFile, loggerLevelController, defaultApp, defaultLevel));
            return configuration;
        }
    }
}