using IFramework.Config;
using IFramework.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IFramework.Log4Net
{
    public static class Log4NetConfiguration
    {
        public static Configuration UseLog4Net(this Configuration configuration,
                                               LogLevel logLevel = LogLevel.Information,
                                               string log4NetConfigFile = "log4net.config")
        {
            IoCFactory.Instance.Populate(UseLog4Net(new ServiceCollection(),
                                                    logLevel,
                                                    log4NetConfigFile));
            return configuration;
        }


        public static IServiceCollection UseLog4Net(this IServiceCollection services,
                                                    LogLevel logLevel = LogLevel.Information,
                                                    string log4NetConfigFile = "log4net.config")
        {
            services.AddLogging(config =>
            {
                var loggerConfiguration = Configuration.Instance.GetSection("logging");
                if (loggerConfiguration == null)
                {
                    config.SetMinimumLevel(logLevel);
                }
                else
                {
                    config.AddConfiguration(loggerConfiguration);
                }
                config.AddProvider(new Log4NetProvider(log4NetConfigFile));
            });
            return services;
        }

        public static void UseLog4Net(this ILoggerFactory loggerFactory, string log4NetConfigFile = "log4net.config")
        {
            loggerFactory.AddProvider(new Log4NetProvider(log4NetConfigFile));
        }
    }
}