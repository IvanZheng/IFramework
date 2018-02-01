using System;
using Microsoft.Extensions.DependencyInjection;

namespace IFramework.DependencyInjection
{
    public interface IObjectProviderBuilder
    {
        IObjectProvider Build(IServiceCollection serviceCollection = null);
        IObjectProviderBuilder RegisterType(Type from, Type to, string name, ServiceLifetime lifetime, params Injection[] injections);

        IObjectProviderBuilder RegisterType(Type from, Type to, ServiceLifetime lifetime, params Injection[] injections);
        IObjectProviderBuilder RegisterType(Type from, Type to, string name = null, params Injection[] injections);

        IObjectProviderBuilder RegisterType<TFrom, TTo>(string name, ServiceLifetime lifetime, params Injection[] injections)
            where TFrom : class where TTo : class, TFrom;
        IObjectProviderBuilder RegisterType<TFrom, TTo>(params Injection[] injections) where TFrom : class where TTo : class, TFrom;


        IObjectProviderBuilder RegisterType<TFrom, TTo>(string name, params Injection[] injections) where TFrom : class where TTo : class, TFrom;

        IObjectProviderBuilder RegisterType<TFrom, TTo>(ServiceLifetime lifetime, params Injection[] injections)
            where TFrom : class where TTo : class, TFrom;


        IObjectProviderBuilder RegisterInstance(Type t, string name, object instance);

        IObjectProviderBuilder RegisterInstance(Type t, object instance);

        IObjectProviderBuilder RegisterInstance<TInterface>(TInterface instance)
            where TInterface : class;

        IObjectProviderBuilder RegisterInstance<TInterface>(string name, TInterface instance) where TInterface : class;
    }
}