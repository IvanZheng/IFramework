using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IFramework.Log4Net
{
    public static class Log4NetConfiguration
    {
        public static Configuration UseLog4Net(this Configuration configuration, string log4NetConfigFile = "log4net.config")
        {
#if FULL_NET_FRAMEWORK
            Utility.SetEntryAssembly();
#endif
            IoCFactory.Instance.Populate(UseLog4Net(new ServiceCollection(), log4NetConfigFile));
            return configuration;
        }


        public static IServiceCollection UseLog4Net(this IServiceCollection services, string log4NetConfigFile = "log4net.config")
        {
            services.AddLogging(config =>
            {
#if FULL_NET_FRAMEWORK
                config.AddConfiguration(Configuration.Instance.ConfigurationCore);
#endif
                config.AddProvider(new Log4NetProvider(log4NetConfigFile, null));
            });
            return services;
        }
    }
}