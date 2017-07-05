using IFramework.Infrastructure;
using IFramework.IoC;
using IFramework.JsonNet;

namespace IFramework.Config
{
    public static class FrameworkConfigurationExtension
    {
        /// <summary>
        ///     Use Log4Net as the logger for the enode framework.
        /// </summary>
        /// <returns></returns>
        public static Configuration UseJsonNet(this Configuration configuration)
        {
            IoCFactory.Instance.CurrentContainer
                .RegisterInstance(typeof(IJsonConvert)
                    , new JsonConvertImpl());
            return configuration;
        }
    }
}