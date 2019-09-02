using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Autofac;
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

        public static IServiceCollection AddAutofacContainer(this IServiceCollection services,
                                                             Func<Assembly, bool> assemblyNameExpression)
        {
            return services.AddAutofacContainer(GetAssemblies(assemblyNameExpression));
        }

        public static IServiceCollection AddAutofacContainer(this IServiceCollection services)
        {
            return services.AddAutofacContainer((Assembly[]) null);
        }

        public static IServiceCollection AddAutofacContainer(this IServiceCollection services, ContainerBuilder builder, params Assembly[] assemblies)
        {
            if (assemblies?.Length > 0)
            {
                builder.RegisterAssemblyTypes(assemblies);
            }

            ObjectProviderFactory.Instance.SetProviderBuilder(new ObjectProviderBuilder(builder));
            return services;
        }

        public static IServiceCollection AddAutofacContainer(this IServiceCollection services,
                                                             params string[] assemblies)
        {
            return services.AddAutofacContainer(assemblies.Select(Assembly.Load).ToArray());
        }

        public static IServiceCollection AddAutofacContainer(this IServiceCollection services,
                                                             params Assembly[] assemblies)
        {
            var builder = new ContainerBuilder();
            if (assemblies?.Length > 0)
            {
                builder.RegisterAssemblyTypes(assemblies);
            }

            ObjectProviderFactory.Instance.SetProviderBuilder(new ObjectProviderBuilder(builder));
            return services;
        }
    }
}