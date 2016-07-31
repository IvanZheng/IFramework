
using System;
using IFramework.Autofac;
using Autofac;
using Autofac.Configuration;
using System.Linq;
using System.Reflection;

namespace IFramework.Config
{
    public static class IFrameworkConfigurationExtension
    {
        public static Configuration RegisterAssemblyTypes(this Configuration configuration, params string[] assemblyNames)
        {
            if (assemblyNames != null && assemblyNames.Length > 0)
            {
                var builder = new ContainerBuilder();
                var assemblies = assemblyNames.Select(name => Assembly.Load(name));
                builder.RegisterAssemblyTypes(assemblies.ToArray());
                builder.Update(IoC.IoCFactory.Instance.CurrentContainer.GetAutofacContainer().ComponentRegistry);
            }
            return configuration;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public static Configuration UseAutofacContainer(this Configuration configuration, IContainer container = null)
        {
            if (IoC.IoCFactory.IsInit())
            {
                return configuration;
            }
            var builder = new ContainerBuilder();
            if (container == null)
            {
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
