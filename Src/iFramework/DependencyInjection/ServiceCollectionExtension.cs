using System;
using System.ComponentModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace IFramework.DependencyInjection
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddService(this IServiceCollection serviceCollection, Type from, Func<IServiceProvider, object> implementationFactory, ServiceLifetime lifetime = ServiceLifetime.Transient)
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

        public static IServiceCollection AddService(this IServiceCollection serviceCollection, Type from, Type to, ServiceLifetime lifetime = ServiceLifetime.Transient)
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

        public static IServiceCollection AddService<TService, TImplementation>(this IServiceCollection serviceCollection, Func<IServiceProvider, TImplementation> implementationFactory, ServiceLifetime lifetime = ServiceLifetime.Transient)
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

        public static IServiceCollection AddService<TService, TImplementation>(this IServiceCollection serviceCollection, 
                                                                               ServiceLifetime lifetime = ServiceLifetime.Transient,
                                                                               params Injection[] injections)
            where TService : class where TImplementation : class, TService
        {
            if (injections.Length > 0)
            {
                ObjectProviderFactory.Instance.ObjectProviderBuilder.AddRegisterAction(builder => builder.Register<TService, TImplementation>(lifetime, injections));
            }
            else
            {
                if (lifetime == ServiceLifetime.Scoped)
                {
                    ServiceCollectionServiceExtensions.AddScoped<TService, TImplementation>(serviceCollection);
                }
                else if (lifetime == ServiceLifetime.Singleton)
                {
                    ServiceCollectionServiceExtensions.AddSingleton<TService, TImplementation>(serviceCollection);
                }
                else if (lifetime == ServiceLifetime.Transient)
                {
                    ServiceCollectionServiceExtensions.AddTransient<TService, TImplementation>(serviceCollection);
                }
                else
                {
                    throw new InvalidEnumArgumentException(nameof(lifetime));
                }
            }
            return serviceCollection;
        }

        public static IServiceCollection AddCustomOptions<TOptions>(this IServiceCollection services, Action<TOptions> optionAction = null, string sectionName = null)
            where TOptions: class, new()
        {
            if (optionAction != null)
            {
                services.Configure(optionAction);
            }
            else
            {
                services.AddSingleton<IOptions<TOptions>>(provider =>
                {
                    var configuration = provider.GetService<IConfiguration>()?.GetSection(sectionName ?? typeof(TOptions).Name);
                    if (!configuration.Exists())
                    {
                        throw new ArgumentNullException($"{nameof(TOptions)}");
                    }

                    var options = new TOptions();
                    configuration.Bind(options);
                    return new OptionsWrapper<TOptions>(options);
                });
            }
            return services;
        }
        

        #region AOP register extensions

        public static IServiceCollection AddScoped<TFrom, TTo>(this IServiceCollection services, params Injection[] injections)
            where TFrom : class
            where TTo : class, TFrom
        {
            return services.AddService<TFrom, TTo>(ServiceLifetime.Scoped, injections);
        }

        public static IServiceCollection AddSingleton<TFrom, TTo>(this IServiceCollection services, params Injection[] injections)
            where TFrom : class
            where TTo : class, TFrom
        {
            return services.AddService<TFrom, TTo>(ServiceLifetime.Singleton, injections);
        }
        public static IServiceCollection AddTransient<TFrom, TTo>(this IServiceCollection services, params Injection[] injections)
            where TFrom : class
            where TTo : class, TFrom
        {
            return services.AddService<TFrom, TTo>(ServiceLifetime.Transient, injections);
        }
        #endregion

        public static IServiceCollection RegisterMessageHandlers(this IServiceCollection services,
                                                                 string[] handlerProviderNames,
                                                                 ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            ObjectProviderFactory.Instance.ObjectProviderBuilder.RegisterMessageHandlers(handlerProviderNames, lifetime);
            return services;
        }
    }
}