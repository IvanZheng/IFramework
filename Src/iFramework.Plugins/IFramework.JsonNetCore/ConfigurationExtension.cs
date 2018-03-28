using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.Infrastructure;

namespace IFramework.JsonNetCore
{
    public static class FrameworkConfigurationExtension
    {
        /// <summary>
        ///     Use Log4Net as the logger for the enode framework.
        /// </summary>
        /// <returns></returns>
        public static Configuration UseJsonNet(this Configuration configuration)
        {
            ObjectProviderFactory.Instance
                      .ObjectProviderBuilder
                      .RegisterInstance(typeof(IJsonConvert), new JsonConvertImpl());
            return configuration;
        }
    }
}