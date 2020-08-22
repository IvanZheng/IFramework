using Microsoft.Extensions.DependencyInjection;
using System;
using Microsoft.Extensions.Logging;
using IFramework.Config;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Extensions.Logging;

namespace IFramework.Logging.Serilog
{
    public static class Extension
    {
        public static IServiceCollection AddSerilog(this IServiceCollection services, IConfiguration configuration = null)
        {
            services.AddLogging(config =>
            {
                configuration = configuration ?? Configuration.Instance;

                Log.Logger = new LoggerConfiguration()
                             .ReadFrom.Configuration(configuration)
                             .CreateLogger();
                config.AddProvider(new SerilogLoggerProvider(Log.Logger, true));
            });
            return services;
        }
    }
}
