﻿using System;
using IFramework.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IFramework.Logging.Log4Net
{
    public static class Log4NetConfiguration
    {
        public static IServiceCollection AddLog4Net(this IServiceCollection services,
                                                    LogLevel logLevel,
                                                    Log4NetProviderOptions options = null)
        {
            services.AddLogging(config =>
            {
                var loggerConfiguration = Configuration.Instance.GetSection("logging");
                if (loggerConfiguration.Exists())
                {
                    config.AddConfiguration(loggerConfiguration);
                }
                config.SetMinimumLevel(logLevel);
                config.AddProvider(new Log4NetProvider(options ?? new Log4NetProviderOptions()));
            });
            return services;
        }
        public static IServiceCollection AddLog4Net(this IServiceCollection services,
                                                    Log4NetProviderOptions options = null)
        {
            services.AddLogging(config =>
            {
                var loggerConfiguration = Configuration.Instance.GetSection("logging");
                if (loggerConfiguration.Exists())
                {
                    config.AddConfiguration(loggerConfiguration);
                    if (Enum.TryParse<LogLevel>(loggerConfiguration.GetSection("LogLevel")["Default"], out var logLevel))
                    {
                        config.SetMinimumLevel(logLevel);
                    }
                }
                config.AddProvider(new Log4NetProvider(options ?? new Log4NetProviderOptions()));
            });
            return services;
        }

        public static void AddLog4NetProvider(this ILoggerFactory loggerFactory, Log4NetProviderOptions options = null)
        {
            loggerFactory.AddProvider(new Log4NetProvider(options ?? new Log4NetProviderOptions()));
        }
    }
}