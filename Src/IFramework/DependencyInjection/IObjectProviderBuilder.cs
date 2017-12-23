using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace IFramework.DependencyInjection
{
    public interface IObjectProviderBuilder
    {
        IObjectProvider Build(IServiceCollection serviceCollection = null);
        IObjectProviderBuilder RegisterType(Type from, Type to, string name, ServiceLifetime lifetime, params Injection[] injections);

        IObjectProviderBuilder RegisterType(Type from, Type to, ServiceLifetime lifetime, params Injection[] injections);
        IObjectProviderBuilder RegisterType(Type from, Type to, string name = null, params Injection[] injections);

        IObjectProviderBuilder RegisterType<TFrom, TTo>(string name, ServiceLifetime lifetime, params Injection[] injections)
            where TTo : TFrom;

        IObjectProviderBuilder RegisterType<TFrom, TTo>(params Injection[] injections) where TTo : TFrom;
        IObjectProviderBuilder RegisterType<TFrom, TTo>(string name, params Injection[] injections) where TTo : TFrom;
        IObjectProviderBuilder RegisterType<TFrom, TTo>(ServiceLifetime lifetime, params Injection[] injections) 
            where TTo : TFrom;


        IObjectProviderBuilder RegisterInstance(Type t, string name, object instance, ServiceLifetime lifetime = ServiceLifetime.Singleton);

        IObjectProviderBuilder RegisterInstance(Type t, object instance, ServiceLifetime lifetime = ServiceLifetime.Singleton);

        IObjectProviderBuilder RegisterInstance<TInterface>(TInterface instance, ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TInterface : class;

        IObjectProviderBuilder RegisterInstance<TInterface>(string name,
                                                TInterface instance,
                                                     ServiceLifetime lifetime = ServiceLifetime.Singleton) where TInterface : class;
    }
}
