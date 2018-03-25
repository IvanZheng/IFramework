using System;
using System.IO;
using System.Reflection;
using IFramework.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Sample.CommandService.App_Start
{
    public static class MicrosoftExtension
    {
        public static Configuration UseLog4Net(this Configuration configuration, string log4NetConfigFile = "log4net.config")
        {
            SetEntryAssembly();
            var services = new ServiceCollection();
            services.AddLogging(config => config.AddProvider(new Log4NetProvider(Path.Combine(AppDomain.CurrentDomain
                                                                                                       .BaseDirectory,
                                                                                              log4NetConfigFile))));
            return configuration;
        }

        public static void SetEntryAssembly()
        {
            SetEntryAssembly(Assembly.GetCallingAssembly());
        }

        public static void SetEntryAssembly(Assembly assembly)
        {
            AppDomainManager manager = new AppDomainManager();
            FieldInfo entryAssemblyfield = manager.GetType().GetField("m_entryAssembly", BindingFlags.Instance | BindingFlags.NonPublic);
            if (entryAssemblyfield != null)
            {
                entryAssemblyfield.SetValue(manager, assembly);
            }

            AppDomain domain = AppDomain.CurrentDomain;
            FieldInfo domainManagerField = domain.GetType().GetField("_domainManager", BindingFlags.Instance | BindingFlags.NonPublic);
            if (domainManagerField != null)
            {
                domainManagerField.SetValue(domain, manager);
            }
        }
    }
}