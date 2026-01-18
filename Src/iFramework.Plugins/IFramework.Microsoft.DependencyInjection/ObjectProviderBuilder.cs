using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using IFramework.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace IFramework.DependencyInjection.Microsoft
{
    public class ObjectProviderBuilder : IObjectProviderBuilder
    {
        private readonly IServiceCollection _serviceCollection;

        private readonly List<Action<IObjectProviderBuilder>> _registerActions = new List<Action<IObjectProviderBuilder>>();
        public IObjectProviderBuilder AddRegisterAction(Action<IObjectProviderBuilder> action)
        {
            _registerActions.Add(action);
            return this;
        }

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
            _registerActions.ForEach(action => action(this));
            _registerActions.Clear();
            _serviceCollection.AddScoped<IObjectProvider>(provider => new ObjectProvider(provider));
            return new ObjectProvider(_serviceCollection);
        }

        public IObjectProviderBuilder Populate(IServiceCollection serviceCollection)
        {
            _serviceCollection.Add(serviceCollection);
            return this;
        }

        public IObjectProviderBuilder Register<TFrom>(Func<IObjectProvider, TFrom> implementationFactory, ServiceLifetime lifetime)
        {
            _serviceCollection.AddService(typeof(TFrom), provider => implementationFactory(new ObjectProvider(provider)), lifetime);
            return this;
        }

        public IObjectProviderBuilder Register<TFrom>(Func<IObjectProvider, TFrom> implementationFactory, ServiceLifetime lifetime, Action<TFrom> releaseAction)
        {
            throw new NotImplementedException();
        }

        public IObjectProviderBuilder Register<TFrom>(Func<IObjectProvider, TFrom> implementationFactory, ServiceLifetime lifetime, Func<TFrom, ValueTask> releaseFunc)
        {
            throw new NotImplementedException();
        }

        public IObjectProviderBuilder Register(Type from, Type to, string name, ServiceLifetime lifetime, params Injection[] injections)
        {
            throw new NotImplementedException();
        }

        public IObjectProviderBuilder Register(Type from, Type to, ServiceLifetime lifetime, params Injection[] injections)
        {
            if (injections.Length > 0)
            {
                throw new NotImplementedException();
            }
            _serviceCollection.AddService(from, to, lifetime);
            return this;
        }

        public IObjectProviderBuilder Register(Type from, Type to, string name = null, params Injection[] injections)
        {
            if (injections.Length > 0 || name != null)
            {
                throw new NotImplementedException();
            }
            _serviceCollection.AddService(from, to);
            return this;
        }

        public IObjectProviderBuilder Register<TFrom, TTo>(string name, ServiceLifetime lifetime, params Injection[] injections)
            where TFrom : class where TTo : class, TFrom
        {
            throw new NotImplementedException();
        }

        public IObjectProviderBuilder Register<TFrom, TTo>(params Injection[] injections)
            where TFrom : class where TTo : class, TFrom

        {
            if (injections.Length > 0)
            {
                throw new NotImplementedException();
            }
            _serviceCollection.AddService<TFrom, TTo>();
            return this;
        }

        public IObjectProviderBuilder Register<TFrom, TTo>(string name, params Injection[] injections)
            where TFrom : class where TTo : class, TFrom

        {
            throw new NotImplementedException();
        }

        public IObjectProviderBuilder Register<TFrom, TTo>(ServiceLifetime lifetime, params Injection[] injections)
          where TFrom : class where TTo : class, TFrom

        {
            if (injections.Length > 0)
            {
                throw new NotImplementedException();
            }
            _serviceCollection.AddService<TFrom, TTo>(lifetime);
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
