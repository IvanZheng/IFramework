using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace IFramework.JsonNet
{
    public static class FrameworkConfigurationExtension
    {
        /// <summary>
        ///     Use Log4Net as the logger for the enode framework.
        /// </summary>
        /// <returns></returns>
        public static IServiceCollection AddJsonNet(this IServiceCollection services)
        {
            services.AddSingleton(typeof(IJsonConvert), new JsonConvertImpl());
            return services;
        }
    }
}