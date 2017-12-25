﻿using System;
using System.Collections.Generic;
using System.Linq;
using IFramework.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Unity;
using Unity.Extension;

namespace IFramework.DependencyInjection.Unity
{
    public class ObjectProviderBuilder : IObjectProviderBuilder
    {
        private readonly UnityContainer _container;

        public ObjectProviderBuilder()
        {
            _container = new UnityContainer();
        }

        public ObjectProviderBuilder(UnityContainer container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }

        public ObjectProviderBuilder(IServiceCollection serviceCollection = null)
            : this()
        {
            _container.RegisterType(this.GetType(), c => { });
        }

        public IObjectProvider Build(IServiceCollection serviceCollection = null)
        {
            if (serviceCollection != null)
            {
                _container.Populate(serviceCollection);
            }
            var objectProvider = new ObjectProvider();
            _container.RegisterInstance<IObjectProvider>(objectProvider);
            objectProvider.SetScope(_container.Build());
            return objectProvider;
        }

        public IObjectProviderBuilder RegisterInstance(Type t, object instance)
        {
            _container.RegisterInstance(instance)
                             .As(t);
            return this;
            ;
        }

        public IObjectProviderBuilder RegisterInstance(Type t, string name, object instance)
        {
            _container.RegisterInstance(instance)
                             .Named(name, t);
            return this;
        }

        public IObjectProviderBuilder RegisterInstance<TInterface>(TInterface instance)
            where TInterface : class
        {
            _container.RegisterInstance(instance);
            return this;
        }

        public IObjectProviderBuilder RegisterInstance<TInterface>(string name, TInterface instance)
            where TInterface : class
        {
            _container.RegisterInstance(instance)
                             .Named<TInterface>(name);
            return this;
        }


        public IObjectProviderBuilder RegisterType(Type from, Type to, string name = null, params Injection[] injections)
        {
            var injectionMembers = GetInjectionParameters(injections);

            dynamic registrationBuilder;
            if (string.IsNullOrEmpty(name))
            {
                if (to.IsGenericType)
                {
                    registrationBuilder = _container.RegisterGeneric(to).As(from);
                }
                else
                {
                    registrationBuilder = _container.RegisterType(to).As(from);
                }
            }
            else
            {
                if (to.IsGenericType)
                {
                    registrationBuilder = _container.RegisterGeneric(to)
                                                           .Named(name, from)
                                                           .WithParameters(injectionMembers);
                }
                else
                {
                    registrationBuilder = _container.RegisterType(to)
                                                           .Named(name, from)
                                                           .WithParameters(injectionMembers);
                }
            }
            RegisterInterceptor(registrationBuilder, injections);
            return this;
        }

        public IObjectProviderBuilder RegisterType(Type from, Type to, ServiceLifetime lifetime, params Injection[] injections)
        {
            var injectionMembers = GetInjectionParameters(injections);

            dynamic registrationBuilder;
            if (to.IsGenericType)
            {
                registrationBuilder = _container.RegisterGeneric(to)
                                                       .As(from)
                                                       .WithParameters(injectionMembers)
                                                       .InstanceLifetime(lifetime);
            }
            else
            {
                registrationBuilder = _container.RegisterType(to)
                                                       .As(from)
                                                       .WithParameters(injectionMembers)
                                                       .InstanceLifetime(lifetime);
            }

            RegisterInterceptor(registrationBuilder, injections);
            return this;
        }

        public IObjectProviderBuilder RegisterType(Type from,
                                                   Type to,
                                                   string name,
                                                   ServiceLifetime lifetime,
                                                   params Injection[] injections)
        {
            var injectionMembers = GetInjectionParameters(injections);
            dynamic registrationBuilder;
            if (to.IsGenericType)
            {
                registrationBuilder = _container.RegisterGeneric(to)
                                                       .Named(name, from)
                                                       .InstanceLifetime(lifetime)
                                                       .WithParameters(injectionMembers);
            }
            else
            {
                registrationBuilder = _container.RegisterType(to)
                                                       .Named(name, from)
                                                       .InstanceLifetime(lifetime)
                                                       .WithParameters(injectionMembers);
            }

            RegisterInterceptor(registrationBuilder, injections);
            return this;
        }

        public IObjectProviderBuilder RegisterType<TFrom, TTo>(ServiceLifetime lifetime, params Injection[] injections)
            where TFrom : class where TTo : class, TFrom
        {
            var injectionMembers = GetInjectionParameters(injections);

            dynamic registrationBuilder;
            if (typeof(TTo).IsGenericType)
            {
                registrationBuilder = _container.RegisterGeneric(typeof(TTo))
                                                       .As(typeof(TFrom))
                                                       .InstanceLifetime(lifetime)
                                                       .WithParameters(injectionMembers);
            }
            else
            {
                registrationBuilder = _container.RegisterType(typeof(TTo))
                                                       .As(typeof(TFrom))
                                                       .InstanceLifetime(lifetime)
                                                       .WithParameters(injectionMembers);
            }

            RegisterInterceptor(registrationBuilder, injections);
            return this;
        }

        public IObjectProviderBuilder RegisterType<TFrom, TTo>(params Injection[] injections)
            where TFrom : class where TTo : class, TFrom
        {
            var injectionMembers = GetInjectionParameters(injections);
            dynamic registrationBuilder;
            if (typeof(TTo).IsGenericType)
            {
                registrationBuilder = _container.RegisterGeneric(typeof(TTo))
                                                       .As(typeof(TFrom))
                                                       .WithParameters(injectionMembers);
            }
            else
            {
                registrationBuilder = _container.RegisterType(typeof(TTo))
                                                       .As(typeof(TFrom))
                                                       .WithParameters(injectionMembers);
            }

            RegisterInterceptor(registrationBuilder, injections);
            return this;
        }

        public IObjectProviderBuilder RegisterType<TFrom, TTo>(string name, params Injection[] injections)
            where TFrom : class where TTo : class, TFrom
        {
            var injectionMembers = GetInjectionParameters(injections);
            dynamic registrationBuilder;
            if (typeof(TTo).IsGenericType)
            {
                registrationBuilder = _container.RegisterGeneric(typeof(TTo))
                                                       .Named(name, typeof(TFrom))
                                                       .WithParameters(injectionMembers);
            }
            else
            {
                registrationBuilder = _container.RegisterType(typeof(TTo))
                                                       .Named(name, typeof(TFrom))
                                                       .WithParameters(injectionMembers);
            }

            RegisterInterceptor(registrationBuilder, injections);
            return this;
        }

        public IObjectProviderBuilder RegisterType<TFrom, TTo>(string name, ServiceLifetime lifetime, params Injection[] injections)
            where TFrom : class where TTo : class, TFrom
        {
            var injectionMembers = GetInjectionParameters(injections);
            var builder = new ContainerBuilder();
            dynamic registrationBuilder;
            if (typeof(TTo).IsGenericType)
            {
                registrationBuilder = builder.RegisterGeneric(typeof(TTo))
                                             .Named(name, typeof(TFrom))
                                             .InstanceLifetime(lifetime)
                                             .WithParameters(injectionMembers);
            }
            else
            {
                registrationBuilder = builder.RegisterType(typeof(TTo))
                                             .Named(name, typeof(TFrom))
                                             .InstanceLifetime(lifetime)
                                             .WithParameters(injectionMembers);
            }

            RegisterInterceptor(registrationBuilder, injections);
            return this;
        }

        private void RegisterInterceptor(dynamic registrationBuilder, Injection[] injections)
        {
            injections.ForEach(injection =>
            {
                if (injection is InterfaceInterceptorInjection)
                {
                    RegistrationExtensions.EnableInterfaceInterceptors(registrationBuilder);
                }
                else if (injection is VirtualMethodInterceptorInjection)
                {
                    RegistrationExtensions.EnableClassInterceptors(registrationBuilder);
                }
                else if (injection is TransparentProxyInterceptorInjection)
                {
                    throw new NotImplementedException();
                    //RegistrationExtensions.InterceptTransparentProxy(registrationBuilder)
                    //                      .UseWcfSafeRelease();
                }
                else if (injection is InterceptionBehaviorInjection)
                {
                    var interceptorType = ((InterceptionBehaviorInjection) injection).BehaviorType;
                    RegistrationExtensions.InterceptedBy(registrationBuilder, interceptorType);
                }
            });
        }

        private IEnumerable<global::Autofac.Core.Parameter> GetInjectionParameters(Injection[] injections)
        {
            var injectionMembers = new List<global::Autofac.Core.Parameter>();
            injections.ForEach(injection =>
            {
                if (injection is ConstructInjection)
                {
                    var constructInjection = injection as ConstructInjection;
                    injectionMembers.AddRange(constructInjection.Parameters
                                                                .Select(p => new NamedParameter(p.ParameterName, p.ParameterValue)));
                }
                else if (injection is ParameterInjection)
                {
                    var propertyInjection = injection as ParameterInjection;
                    injectionMembers.Add(new NamedPropertyParameter(propertyInjection.ParameterName,
                                                                    propertyInjection.ParameterValue));
                }
            });
            return injectionMembers;
        }
    }
}