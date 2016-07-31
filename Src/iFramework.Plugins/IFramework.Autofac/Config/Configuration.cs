
using System;
using IFramework.Autofac;
using Autofac;
using Autofac.Configuration;

namespace IFramework.Config
{
    public static class IFrameworkConfigurationExtension
    {
        /// <summary>Use Log4Net as the logger for the enode framework.
        /// </summary>
        /// <returns></returns>
        public static Configuration UseAutofacContainer(this Configuration configuration, IContainer container = null)
        {
            if (IoC.IoCFactory.IsInit())
            {
                return configuration;
            }
            if (container == null)
            {
                var builder = new ContainerBuilder();
                try
                {
                    builder.RegisterModule(new ConfigurationSettingsReader("autofac"));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.GetBaseException().Message);
                }
                container = builder.Build();
            }
              
            IoC.IoCFactory.SetContainer(new ObjectContainer(container));
            return configuration;
        }
    }
}
