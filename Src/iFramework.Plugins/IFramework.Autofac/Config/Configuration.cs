using System;
using System.Linq;
using System.Reflection;
using Autofac;
using Autofac.Configuration;
using IFramework.Autofac;
using IFramework.IoC;
using IContainer = Autofac.IContainer;

namespace IFramework.Config
{
    public static class IFrameworkConfigurationExtension
    {
        public static Configuration RegisterAssemblyTypes(this Configuration configuration,
                                                          params string[] assemblyNames)
        {
            if (assemblyNames != null && assemblyNames.Length > 0)
            {
                var assemblies = assemblyNames.Select(name => Assembly.Load(name)).ToArray();
                configuration.RegisterAssemblyTypes(assemblies);
            }
            return configuration;
        }

        public static Configuration RegisterAssemblyTypes(this Configuration configuration,
                                                          params Assembly[] assemblies)
        {
            if (assemblies != null && assemblies.Length > 0)
            {
                var builder = new ContainerBuilder();
                builder.RegisterAssemblyTypes(assemblies);
                builder.Update(IoCFactory.Instance.CurrentContainer.GetAutofacContainer().ComponentRegistry);
            }
            return configuration;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public static Configuration UseAutofacContainer(this Configuration configuration,
                                                        IContainer container = null,
                                                        string configurationSector = "autofac")
        {
            if (IoCFactory.IsInit())
            {
                return configuration;
            }
            var builder = new ContainerBuilder();
            if (container == null)
            {
                try
                {
                    var settingsReader = new ConfigurationSettingsReader(configurationSector);
                    if (settingsReader != null)
                    {
                        builder.RegisterModule(settingsReader);
                    }
                }
                catch (Exception)
                {
                    //Console.WriteLine(ex.GetBaseException().Message);
                }
                builder.RegisterAssemblyTypes(AppDomain.CurrentDomain.GetAssemblies());
                container = builder.Build();
            }
            else
            {
                builder.RegisterAssemblyTypes(AppDomain.CurrentDomain.GetAssemblies());
                builder.Update(container);
            }
            IoCFactory.SetContainer(new ObjectContainer(container));
            return configuration;
        }
    }
}