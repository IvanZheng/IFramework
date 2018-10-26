using System;
using Microsoft.Extensions.DependencyInjection;

namespace IFramework.DependencyInjection
{
    public interface IObjectProviderBuilder
    {
        IObjectProvider Build(IServiceCollection serviceCollection = null);
        IObjectProviderBuilder Populate(IServiceCollection serviceCollection);

        IObjectProviderBuilder Register<TFrom>(Func<IObjectProvider, TFrom> implementationFactory, ServiceLifetime lifetime);

        IObjectProviderBuilder Register(Type from, Type to, string name, ServiceLifetime lifetime, params Injection[] injections);

        IObjectProviderBuilder Register(Type from, Type to, ServiceLifetime lifetime, params Injection[] injections);
        IObjectProviderBuilder Register(Type from, Type to, string name = null, params Injection[] injections);

        IObjectProviderBuilder Register<TFrom, TTo>(string name, ServiceLifetime lifetime, params Injection[] injections)
            where TFrom : class where TTo : class, TFrom;
        IObjectProviderBuilder Register<TFrom, TTo>(params Injection[] injections) where TFrom : class where TTo : class, TFrom;


        IObjectProviderBuilder Register<TFrom, TTo>(string name, params Injection[] injections) where TFrom : class where TTo : class, TFrom;

        IObjectProviderBuilder Register<TFrom, TTo>(ServiceLifetime lifetime, params Injection[] injections)
            where TFrom : class where TTo : class, TFrom;


        IObjectProviderBuilder RegisterInstance(Type t, string name, object instance);

        IObjectProviderBuilder RegisterInstance(Type t, object instance);

        IObjectProviderBuilder RegisterInstance<TInterface>(TInterface instance)
            where TInterface : class;

        IObjectProviderBuilder RegisterInstance<TInterface>(string name, TInterface instance) where TInterface : class;
    }
}