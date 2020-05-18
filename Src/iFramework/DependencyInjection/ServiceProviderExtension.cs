using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace IFramework.DependencyInjection
{
    public static class ServiceProviderExtension
    {
        public static TService GetService<TService>(this IServiceProvider provider, string name, params Parameter[] parameters) where TService : class
        {
            return provider.GetService<IObjectProvider>()
                           .GetService<TService>(name, parameters);
        }

        public static object GetService(this IServiceProvider provider, Type type, string name, params Parameter[] parameters)
        {
            return provider.GetService<IObjectProvider>()
                           .GetService(type, name, parameters);
        }
    }
}
