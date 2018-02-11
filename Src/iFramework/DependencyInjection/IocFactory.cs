using System;
using System.ComponentModel;
using IFramework.Event.Impl;
using Microsoft.Extensions.DependencyInjection;

namespace IFramework.DependencyInjection
{
    public sealed class IoCFactory
    {
        #region Singleton

        /// <summary>
        ///     Get singleton instance of IoCFactory
        /// </summary>
        public static IoCFactory Instance { get; } = new IoCFactory();

        #endregion

        #region Members

        private IObjectProviderBuilder _objectProviderBuilder;
        public IObjectProviderBuilder ObjectProviderBuilder
        {
            get
            {
                if (_objectProviderBuilder == null)
                {
                    throw new Exception("Please SetProviderBuilder first.");
                }
                return _objectProviderBuilder;
            }
            set => _objectProviderBuilder = value;
        }

        private IObjectProvider _objectProvider;
        public IObjectProvider ObjectProvider
        {
            get
            {
                if (_objectProvider == null)
                {
                    throw new Exception("Please call Build first.");
                }
                return _objectProvider;
            }
        }

        public IObjectProviderBuilder SetProviderBuilder(IObjectProviderBuilder objectProviderBuilder)
        {
            return ObjectProviderBuilder = objectProviderBuilder;
        }

        public IObjectProvider Build(IServiceCollection serviceCollection = null)
        {
            return _objectProvider = ObjectProviderBuilder.Build(serviceCollection);
        }

        #endregion


        public static IObjectProvider CreateScope()
        {
            return Instance.ObjectProvider.CreateScope();
        }

        public IObjectProvider CreateScope(IServiceCollection serviceCollection)
        {
            return Instance.ObjectProvider.CreateScope(serviceCollection);
        }

        public IObjectProvider CreateScope(Action<IObjectProviderBuilder> buildAction)
        {
            return Instance.ObjectProvider.CreateScope(buildAction);
        }

        public static T GetService<T>(string name, params Parameter[] parameters) where T : class
        {
            return Instance.ObjectProvider.GetService<T>(name, parameters);
        }

        public static T GetService<T>(params Parameter[] parameters) where T : class
        {
            return Instance.ObjectProvider.GetService<T>(parameters);
        }

        public static object GetService(Type type, params Parameter[] parameters)
        {
            return Instance.ObjectProvider.GetService(type, parameters);
        }



        public static object GetService(Type type, string name, params Parameter[] parameters)
        {
            return Instance.ObjectProvider.GetService(type, name, parameters);
        }

        public IObjectProviderBuilder RegisterInstance(Type type, object instance)
        {
            ObjectProviderBuilder.RegisterInstance(type, instance);
            return ObjectProviderBuilder;
        }

        public IObjectProviderBuilder RegisterInstance(object instance)
        {
            ObjectProviderBuilder.RegisterInstance(instance.GetType(), instance);
            return ObjectProviderBuilder;
        }

        public IObjectProviderBuilder RegisterType<TService>(Func<IObjectProvider, TService> implementationFactory, ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class
        {
            ObjectProviderBuilder.Register(implementationFactory, lifetime);
            return ObjectProviderBuilder;
        }

        public IObjectProviderBuilder RegisterType<TService, TImplementation>(ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class where TImplementation : class, TService
        {
            ObjectProviderBuilder.Register<TService, TImplementation>(lifetime);
            return ObjectProviderBuilder;
        }

        public IoCFactory RegisterComponents(Action<IObjectProviderBuilder, ServiceLifetime> registerComponents,
                                             ServiceLifetime lifetime = ServiceLifetime.Transient)
        {
            registerComponents(ObjectProviderBuilder, lifetime);
            return Instance;
        }

        public IoCFactory Populate(IServiceCollection services)
        {
            ObjectProviderBuilder.Populate(services);
            return this;
        }
    }
}