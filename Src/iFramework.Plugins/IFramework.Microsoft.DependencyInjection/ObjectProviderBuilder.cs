using System;
using System.Collections.Generic;
using System.Text;
using IFramework.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace IFramework.DependencyInjection.Microsoft
{
    public class ObjectProviderBuilder: IObjectProviderBuilder
    {
        private readonly IServiceCollection _serviceCollection;
        public ObjectProviderBuilder(IServiceCollection serviceCollection = null)
        {
            _serviceCollection = serviceCollection ?? new ServiceCollection();
        }

        public IObjectProvider Build(IServiceCollection serviceCollection = null)
        {
            _serviceCollection.Add(serviceCollection);
            return new ObjectProvider(_serviceCollection);
        }

        public IObjectProviderBuilder RegisterType(Type from, Type to, string name, ServiceLifetime lifetime, params Injection[] injections)
        {
            throw new NotImplementedException();
        }

        public IObjectProviderBuilder RegisterType(Type from, Type to, ServiceLifetime lifetime, params Injection[] injections)
        {
            if (injections != null)
            {
                throw new NotImplementedException();
            }
            return this;
        }

        public IObjectProviderBuilder RegisterType(Type from, Type to, string name = null, params Injection[] injections)
        {
            throw new NotImplementedException();
        }

        public IObjectProviderBuilder RegisterType<TFrom, TTo>(string name, ServiceLifetime lifetime, params Injection[] injections) where TTo : TFrom
        {
            throw new NotImplementedException();
        }

        public IObjectProviderBuilder RegisterType<TFrom, TTo>(params Injection[] injections) where TTo : TFrom
        {
            throw new NotImplementedException();
        }

        public IObjectProviderBuilder RegisterType<TFrom, TTo>(string name, params Injection[] injections) where TTo : TFrom
        {
            throw new NotImplementedException();
        }

        public IObjectProviderBuilder RegisterType<TFrom, TTo>(ServiceLifetime lifetime, params Injection[] injections) where TTo : TFrom
        {
            throw new NotImplementedException();
        }

        public IObjectProviderBuilder RegisterInstance(Type t, string name, object instance, ServiceLifetime lifetime = ServiceLifetime.Singleton)
        {
            throw new NotImplementedException();
        }

        public IObjectProviderBuilder RegisterInstance(Type t, object instance, ServiceLifetime lifetime = ServiceLifetime.Singleton)
        {
            throw new NotImplementedException();
        }

        public IObjectProviderBuilder RegisterInstance<TInterface>(TInterface instance, ServiceLifetime lifetime = ServiceLifetime.Singleton) where TInterface : class
        {
            throw new NotImplementedException();
        }

        public IObjectProviderBuilder RegisterInstance<TInterface>(string name, TInterface instance, ServiceLifetime lifetime = ServiceLifetime.Singleton) where TInterface : class
        {
            throw new NotImplementedException();
        }
    }
}
