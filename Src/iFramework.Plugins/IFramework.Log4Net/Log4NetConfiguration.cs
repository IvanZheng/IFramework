using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IFramework.Log4Net
{
    public static class Log4NetConfiguration
    {
        public static Configuration UseLog4Net(this Configuration configuration, LogLevel logLevel = LogLevel.Debug, string log4NetConfigFile = "log4net.config")
        {
            IoCFactory.Instance.Populate(UseLog4Net(new ServiceCollection(), logLevel, log4NetConfigFile));
            return configuration;
        }


        public static IServiceCollection UseLog4Net(this IServiceCollection services,
                                                    LogLevel logLevel = LogLevel.Debug,
                                                    string log4NetConfigFile = "log4net.config")
        {
            services.AddLogging(config =>
            {
                config.AddProvider(new Log4NetProvider(log4NetConfigFile));
                config.SetMinimumLevel(logLevel);
            });
            return services;
        }
    }
}