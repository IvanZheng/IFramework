using System;
using System.Collections.Generic;
using Aliyun.Api.LogService.Domain.Log;
using IFramework.Config;
using IFramework.Logging.Abstracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IFramework.Logging.AliyunLog
{
    public static class Extension
    {
        public static IServiceCollection AddAliyunLog(this IServiceCollection services, 
                                                      LogLevel minLevel = LogLevel.Warning,
                                                      Func<IEnumerable<LogEvent>, LogGroupInfo> getLogGroupInfo = null,
                                                      Action<AliyunLogOptions> options = null,
                                                      IConfiguration configuration = null,
                                                      bool asyncLog = true,
                                                      int batchCount = 100)
        {
            services.AddLogging(config =>
            {
                configuration = configuration ?? Configuration.Instance;
                
                var providerOptions = new AliyunLogOptions();
                if (options != null)
                {
                    providerOptions = new AliyunLogOptions();
                    options.Invoke(providerOptions);
                }
                else
                {
                    var section = configuration.GetSection(nameof(AliyunLogOptions));
                    if (section.Exists())
                    {
                        providerOptions = section.Get<AliyunLogOptions>();
                    }
                }

                config.AddProvider(new AliyunLoggerProvider(providerOptions, minLevel, asyncLog, getLogGroupInfo, batchCount));
            });
            return services;
        }
    }
}