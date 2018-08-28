using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Autofac;
using IFramework.Config;
using Microsoft.Extensions.DependencyInjection;

namespace IFramework.DependencyInjection.Autofac
{
    public static class ConfigurationExtension
    {
        public static Assembly[] GetAssemblies(Func<Assembly, bool> assemblyNameExpression)
        {
            var assemblyFiles = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory)
                                         .Select(f => new FileInfo(f))
                                         .ToArray();

            var assemblies = assemblyFiles.Where(f => f.Name.EndsWith(".dll")
                                                      && !f.Name.EndsWith(".PrecompiledViews.dll")
                                                      && !f.Name.EndsWith(".Views.dll"))
                                          .Select(f => Assembly.Load(f.Name.Remove(f.Name.Length - 4)))
                                          .Where(assemblyNameExpression)
                                          .ToArray();

            var loadedAssemblies = AppDomain.CurrentDomain
                                            .GetAssemblies()
                                            .Where(assembly => !assembly.GetName().Name.EndsWith(".PrecompiledViews")
                                                               && !assembly.GetName().Name.EndsWith(".Views"))
                                            .Where(assemblyNameExpression);

            return loadedAssemblies.Union(assemblies)
                                   .ToArray();
        }

        public static Configuration UseAutofacContainer(this Configuration configuration,
                                                        Func<Assembly, bool> assemblyNameExpression)
        {
            return configuration.UseAutofacContainer(GetAssemblies(assemblyNameExpression));
        }

        public static Configuration UseAutofacContainer(this Configuration configuration)
        {
            return configuration.UseAutofacContainer((Assembly[]) null);
        }

        public static Configuration UseAutofacContainer(this Configuration configuration,
                                                        params string[] assemblies)
        {
            return configuration.UseAutofacContainer(assemblies.Select(Assembly.Load).ToArray());
        }

        public static Configuration UseAutofacContainer(this Configuration configuration,
                                                        params Assembly[] assemblies)
        {
            var builder = new ContainerBuilder();
            if (assemblies?.Length > 0)
            {
                builder.RegisterAssemblyTypes(assemblies);
            }
            ObjectProviderFactory.Instance.SetProviderBuilder(new ObjectProviderBuilder(builder));
            return configuration;
        }


        public static Configuration UseAutofacContainer(this Configuration configuration,
                                                        ContainerBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }
            ObjectProviderFactory.Instance.SetProviderBuilder(new ObjectProviderBuilder(builder));
            return configuration;
        }

        public static Configuration UseAutofacContainer(this Configuration configuration,
                                                        IServiceCollection serviceCollection)
        {
            ObjectProviderFactory.Instance.SetProviderBuilder(new ObjectProviderBuilder(serviceCollection));
            return configuration;
        }
    }
}