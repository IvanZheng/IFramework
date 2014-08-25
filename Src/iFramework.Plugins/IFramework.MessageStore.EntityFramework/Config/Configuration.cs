using IFramework.Config;
using IFramework.Infrastructure;
using IFramework.Message;
using Microsoft.Practices.Unity;
using System;


namespace IFramework.Config
{
    public static class IFrameworkConfigurationExtension
    {
        /// <summary>Use Log4Net as the logger for the enode framework.
        /// </summary>
        /// <returns></returns>
        public static Configuration RegisterMessageContextType(this Configuration configuration, Type messageContextType, InjectionConstructor injectionConstructor = null)
        {
            if (injectionConstructor == null)
            {
                injectionConstructor = new InjectionConstructor(new ResolvedParameter(typeof(IMessage), "message"));
            }
            IoCFactory.Instance.CurrentContainer
                               .RegisterType(typeof(IMessageContext),
                                             messageContextType,
                                             "MessageStoreMapping",
                                             injectionConstructor);
                              
            return configuration;
        }
    }
}
