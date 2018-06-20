﻿using System.Linq;
using IFramework.Config;
using IFramework.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IFramework.Log4Net
{
    public static class Log4NetConfiguration
    {
        public static Configuration UseLog4Net(this Configuration configuration,
                                               LogLevel logLevel = LogLevel.Information,
                                               Log4NetProviderOptions options = null)
        {
            ObjectProviderFactory.Instance.Populate(UseLog4Net(new ServiceCollection(),
                                                    logLevel,
                                                    options));
            return configuration;
        }


        public static IServiceCollection UseLog4Net(this IServiceCollection services,
                                                    LogLevel logLevel = LogLevel.Information,
                                                    Log4NetProviderOptions options = null)
        {
            services.AddLogging(config =>
            {
                var loggerConfiguration = Configuration.Instance.GetSection("logging");
                if (loggerConfiguration.Exists())
                {
                    config.AddConfiguration(loggerConfiguration);
                }
                else
                {
                    config.SetMinimumLevel(logLevel);
                }
                config.AddProvider(new Log4NetProvider(options));
            });
            return services;
        }

        public static void UseLog4Net(this ILoggerFactory loggerFactory, Log4NetProviderOptions options = null)
        {
            loggerFactory.AddProvider(new Log4NetProvider(options));
        }
    }
}