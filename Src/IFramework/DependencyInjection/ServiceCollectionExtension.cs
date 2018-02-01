using System;
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace IFramework.DependencyInjection
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection RegisterType(this IServiceCollection serviceCollection, Type from, Func<IServiceProvider, object> implementationFactory, ServiceLifetime lifetime = ServiceLifetime.Transient)
        {
            if (lifetime == ServiceLifetime.Scoped)
            {
                serviceCollection.AddScoped(from, implementationFactory);
            }
            else if (lifetime == ServiceLifetime.Singleton)
            {
                serviceCollection.AddSingleton(from, implementationFactory);
            }
            else if (lifetime == ServiceLifetime.Transient)
            {
                serviceCollection.AddTransient(from, implementationFactory);
            }
            else
            {
                throw new InvalidEnumArgumentException(nameof(lifetime));
            }
            return serviceCollection;
        }

        public static IServiceCollection RegisterType(this IServiceCollection serviceCollection, Type from, Type to, ServiceLifetime lifetime = ServiceLifetime.Transient)
        {
            if (lifetime == ServiceLifetime.Scoped)
            {
                serviceCollection.AddScoped(from, to);
            }
            else if (lifetime == ServiceLifetime.Singleton)
            {
                serviceCollection.AddSingleton(from, to);
            }
            else if (lifetime == ServiceLifetime.Transient)
            {
                serviceCollection.AddTransient(from, to);
            }
            else
            {
                throw new InvalidEnumArgumentException(nameof(lifetime));
            }
            return serviceCollection;
        }

        public static IServiceCollection RegisterType<TService, TImplementation>(this IServiceCollection serviceCollection, Func<IServiceProvider, TImplementation> implementationFactory, ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class where TImplementation : class, TService
        {
            if (lifetime == ServiceLifetime.Scoped)
            {
                serviceCollection.AddScoped(implementationFactory);
            }
            else if (lifetime == ServiceLifetime.Singleton)
            {
                serviceCollection.AddSingleton(implementationFactory);
            }
            else if (lifetime == ServiceLifetime.Transient)
            {
                serviceCollection.AddTransient(implementationFactory);
            }
            else
            {
                throw new InvalidEnumArgumentException(nameof(lifetime));
            }
            return serviceCollection;
        }

        public static IServiceCollection RegisterType<TService, TImplementation>(this IServiceCollection serviceCollection, ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class where TImplementation : class, TService
        {
            if (lifetime == ServiceLifetime.Scoped)
            {
                serviceCollection.AddScoped<TService, TImplementation>();
            }
            else if (lifetime == ServiceLifetime.Singleton)
            {
                serviceCollection.AddSingleton<TService, TImplementation>();
            }
            else if (lifetime == ServiceLifetime.Transient)
            {
                serviceCollection.AddTransient<TService, TImplementation>();
            }
            else
            {
                throw new InvalidEnumArgumentException(nameof(lifetime));
            }
            return serviceCollection;
        }
    }
}