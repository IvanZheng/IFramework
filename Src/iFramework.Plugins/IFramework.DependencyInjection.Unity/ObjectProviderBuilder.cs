using System;
using System.Collections.Generic;
using System.Linq;
using IFramework.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Unity;
using Unity.Injection;
using Unity.Interception.ContainerIntegration;
using Unity.Interception.Interceptors.InstanceInterceptors.InterfaceInterception;
using Unity.Interception.Interceptors.TypeInterceptors.VirtualMethodInterception;
using Unity.Lifetime;
using Unity.Microsoft.DependencyInjection;
using Unity.Registration;

namespace IFramework.DependencyInjection.Unity
{
    public class ObjectProviderBuilder : IObjectProviderBuilder
    {
        private static readonly string LifetimeManagerKeyFormat = "IoC.{0}";

        private readonly UnityContainer _container;

        public ObjectProviderBuilder(UnityContainer container = null)
        {
            _container = container ?? new UnityContainer();

            _container.AddNewExtension<Interception>();

            #region register lifetimemanager

            _container.RegisterType<LifetimeManager, ContainerControlledLifetimeManager>(GetLifetimeManagerKey(ServiceLifetime.Singleton));
            _container.RegisterType<LifetimeManager, HierarchicalLifetimeManager>(GetLifetimeManagerKey(ServiceLifetime.Scoped));
            _container.RegisterType<LifetimeManager, TransientLifetimeManager>(GetLifetimeManagerKey(ServiceLifetime.Transient));

            #endregion
        }

        public IObjectProvider Build(IServiceCollection serviceCollection = null)
        {
            if (serviceCollection != null)
            {
                _container.BuildServiceProvider(serviceCollection);
            }

            Register<IObjectProvider>(context =>
            {
                var provider = context as ObjectProvider;
                if (provider == null)
                {
                    throw new Exception("object provider is not Unity ObjectProvider!");
                }
                return new ObjectProvider(provider.UnityContainer);
            }, ServiceLifetime.Scoped);
            var objectProvider = new ObjectProvider(_container);
            return objectProvider;
        }

        public IObjectProviderBuilder Populate(IServiceCollection serviceCollection)
        {
            _container.BuildServiceProvider(serviceCollection);
            return this;
        }

        public IObjectProviderBuilder Register<TFrom>(Func<IObjectProvider, TFrom> implementationFactory, ServiceLifetime lifetime)
        {
            _container.RegisterType<TFrom>(GetLifeTimeManager(lifetime),
                                           new InjectionFactory(container => implementationFactory(new ObjectProvider(container as UnityContainer))));
            return this;
        }

        public IObjectProviderBuilder Register(Type from, Type to, string name, ServiceLifetime lifetime, params Injection[] injections)
        {
            _container.RegisterType(from, to, name, GetLifeTimeManager(lifetime), GetInjectionParameters(from, injections));
            return this;
        }

        public IObjectProviderBuilder Register(Type from, Type to, ServiceLifetime lifetime, params Injection[] injections)
        {
            _container.RegisterType(from, to, GetLifeTimeManager(lifetime), GetInjectionParameters(from, injections));
            return this;
        }

        public IObjectProviderBuilder Register(Type from, Type to, string name = null, params Injection[] injections)
        {
            _container.RegisterType(from, to, name, GetInjectionParameters(from, injections));
            return this;
        }

        public IObjectProviderBuilder Register<TFrom, TTo>(string name, ServiceLifetime lifetime, params Injection[] injections) where TFrom : class where TTo : class, TFrom
        {
            _container.RegisterType<TFrom, TTo>(GetLifeTimeManager(lifetime), GetInjectionParameters(typeof(TFrom), injections));
            return this;
        }

        public IObjectProviderBuilder Register<TFrom, TTo>(params Injection[] injections) where TFrom : class where TTo : class, TFrom
        {
            _container.RegisterType<TFrom, TTo>(GetInjectionParameters(typeof(TFrom), injections));
            return this;
        }

        public IObjectProviderBuilder Register<TFrom, TTo>(string name, params Injection[] injections) where TFrom : class where TTo : class, TFrom
        {
            _container.RegisterType<TFrom, TTo>(name, GetInjectionParameters(typeof(TFrom), injections));
            return this;
        }

        public IObjectProviderBuilder Register<TFrom, TTo>(ServiceLifetime lifetime, params Injection[] injections) where TFrom : class where TTo : class, TFrom
        {
            _container.RegisterType<TFrom, TTo>(GetLifeTimeManager(lifetime), GetInjectionParameters(typeof(TFrom), injections));
            return this;
        }

        public IObjectProviderBuilder RegisterInstance(Type t, string name, object instance)
        {
            _container.RegisterInstance(t, name, instance);
            return this;
        }

        public IObjectProviderBuilder RegisterInstance(Type t, object instance)
        {
            _container.RegisterInstance(t, instance);
            return this;
        }

        public IObjectProviderBuilder RegisterInstance<TInterface>(TInterface instance) where TInterface : class
        {
            _container.RegisterInstance(instance);
            return this;
        }

        public IObjectProviderBuilder RegisterInstance<TInterface>(string name, TInterface instance) where TInterface : class
        {
            _container.RegisterInstance(name, instance);
            return this;
        }

        public static string GetLifetimeManagerKey(ServiceLifetime lifetime)
        {
            return string.Format(LifetimeManagerKeyFormat, lifetime);
        }

        private LifetimeManager GetLifeTimeManager(ServiceLifetime lifetime)
        {
            LifetimeManager lifetimeManager = null;
            lifetimeManager = _container.Resolve<LifetimeManager>(GetLifetimeManagerKey(lifetime));
            if (lifetimeManager == null)
            {
                throw new Exception($"{lifetime} is not supported.");
            }

            return lifetimeManager;
        }

        private InjectionMember[] GetInjectionParameters(Type from, Injection[] injections)
        {
            var injectionMembers = new List<InjectionMember>();
            injections.ForEach(injection =>
            {
                if (injection is ConstructInjection)
                {
                    var constructInjection = (ConstructInjection) injection;
                    injectionMembers.Add(new InjectionConstructor(constructInjection.Parameters
                                                                                    .Select(p => p.ParameterValue)
                                                                                    .ToArray()));
                }
                else if (injection is ParameterInjection)
                {
                    var propertyInjection = (ParameterInjection) injection;
                    injectionMembers.Add(new InjectionProperty(propertyInjection.ParameterName,
                                                               propertyInjection.ParameterValue));
                }
                else if (injection is InterceptionBehaviorInjection)
                {
                    var behaviorType = ((InterceptionBehaviorInjection) injection).BehaviorType;
                    var interceptorType = behaviorType ?? typeof(DefaultInterceptor);
                    injectionMembers.Add(new InterceptionBehavior(interceptorType));
                }
                else if (injection is InterfaceInterceptorInjection)
                {
                    injectionMembers.Add(new Interceptor<InterfaceInterceptor>());
                }
                else if (injection is VirtualMethodInterceptorInjection)
                {
                    injectionMembers.Add(new Interceptor<VirtualMethodInterceptor>());
                }
            });
            return injectionMembers.ToArray();
        }
    }
}