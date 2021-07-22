using System;
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
                                                      Action<AliyunLogOptions> options = null,
                                                      IConfiguration configuration = null)
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

                config.AddProvider(new AliyunLoggerProvider(providerOptions, minLevel));
            });
            return services;
        }
    }
}
