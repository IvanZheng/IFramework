using System;
using System.Collections.Generic;
using System.Text;
using IFramework.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace IFramework.DependencyInjection.Microsoft
{
    public class ObjectProviderBuilder : IObjectProviderBuilder
    {
        private readonly IServiceCollection _serviceCollection;
        public ObjectProviderBuilder(IServiceCollection serviceCollection = null)
        {
            _serviceCollection = serviceCollection ?? new ServiceCollection();
        }

        public IObjectProvider Build(IServiceCollection serviceCollection = null)
        {
            if (serviceCollection != null)
            {
                _serviceCollection.Add(serviceCollection);
            }
            return new ObjectProvider(_serviceCollection);
        }

        public IObjectProviderBuilder RegisterType(Type from, Type to, string name, ServiceLifetime lifetime, params Injection[] injections)
        {
            throw new NotImplementedException();
        }

        public IObjectProviderBuilder RegisterType(Type from, Type to, ServiceLifetime lifetime, params Injection[] injections)
        {
            if (injections.Length > 0)
            {
                throw new NotImplementedException();
            }
            _serviceCollection.RegisterType(from, to, lifetime);
            return this;
        }

        public IObjectProviderBuilder RegisterType(Type from, Type to, string name = null, params Injection[] injections)
        {
            if (injections.Length > 0 || name != null)
            {
                throw new NotImplementedException();
            }
            _serviceCollection.RegisterType(from, to);
            return this;
        }

        public IObjectProviderBuilder RegisterType<TFrom, TTo>(string name, ServiceLifetime lifetime, params Injection[] injections)
            where TFrom : class where TTo : class, TFrom
        {
            throw new NotImplementedException();
        }

        public IObjectProviderBuilder RegisterType<TFrom, TTo>(params Injection[] injections)
            where TFrom : class where TTo : class, TFrom

        {
            if (injections.Length > 0)
            {
                throw new NotImplementedException();
            }
            _serviceCollection.RegisterType<TFrom, TTo>();
            return this;
        }

        public IObjectProviderBuilder RegisterType<TFrom, TTo>(string name, params Injection[] injections)
            where TFrom : class where TTo : class, TFrom

        {
            throw new NotImplementedException();
        }

        public IObjectProviderBuilder RegisterType<TFrom, TTo>(ServiceLifetime lifetime, params Injection[] injections)
          where TFrom : class where TTo : class, TFrom

        {
            if (injections.Length > 0)
            {
                throw new NotImplementedException();
            }
            _serviceCollection.RegisterType<TFrom, TTo>(lifetime);
            return this;
        }

        public IObjectProviderBuilder RegisterInstance(Type t, string name, object instance)
        {
            throw new NotImplementedException();
        }

        public IObjectProviderBuilder RegisterInstance(Type t, object instance)
        {
            _serviceCollection.AddSingleton(t, instance);
            return this;
        }

        public IObjectProviderBuilder RegisterInstance<TInterface>(TInterface instance) where TInterface : class
        {
            _serviceCollection.AddSingleton(instance);
            return this;
        }

        public IObjectProviderBuilder RegisterInstance<TInterface>(string name, TInterface instance) where TInterface : class
        {
            throw new NotImplementedException();
        }
    }
}
