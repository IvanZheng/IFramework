using IFramework.Infrastructure;
using IFramework.IoC;
using Microsoft.Practices.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.Unity
{
    public static class ObjectContainerExtension
    {
        public static IUnityContainer GetUnityContainer(this IContainer objectContainer)
        {
            return (objectContainer as ObjectContainer)?._unityContainer;
        }
    }

    public class ObjectContainer : IContainer
    {
        internal IUnityContainer _unityContainer;
        public ObjectContainer(IUnityContainer unityContainer)
        {
            _unityContainer = unityContainer;
        }

        public object ContainerInstanse
        {
            get
            {
                return _unityContainer;
            }
        }

        public IContainer Parent
        {
            get
            {
                return new ObjectContainer(_unityContainer.Parent);
            }
        }

        public IContainer CreateChildContainer()
        {
            return new ObjectContainer(_unityContainer.CreateChildContainer());
        }

        public void Dispose()
        {
            _unityContainer.Dispose();
        }

        public IContainer RegisterInstance(Type t, object instance, Lifetime life = Lifetime.Singleton)
        {
            _unityContainer.RegisterInstance(t, instance, GetLifeTimeManager(life));
            return this;
        }

        public IContainer RegisterInstance(Type t, string name, object instance, Lifetime life = Lifetime.Singleton)
        {
            _unityContainer.RegisterInstance(t, name, instance, GetLifeTimeManager(life));
            return this;
        }

        public IContainer RegisterInstance<TInterface>(TInterface instance, Lifetime life = Lifetime.Singleton)
        {
            _unityContainer.RegisterInstance(instance, GetLifeTimeManager(life));
            return this;
        }

        public IContainer RegisterInstance<TInterface>(string name, TInterface instance, Lifetime life = Lifetime.Singleton)
        {
            _unityContainer.RegisterInstance<TInterface>(name, instance, GetLifeTimeManager(life));
            return this;
        }


        public IContainer RegisterType(Type from, Type to, string name = null, params Injection[] injections)
        {
            var injectionMembers = GetInjectionParameters(injections);
            if (string.IsNullOrEmpty(name))
            {
                _unityContainer.RegisterType(from, to, injectionMembers);
            }
            else
            {
                _unityContainer.RegisterType(from, to, name, injectionMembers);
            }
            return this;
        }

        public IContainer RegisterType(Type from, Type to, Lifetime lifetime, params Injection[] injections)
        {
            var injectionMembers = GetInjectionParameters(injections);
            _unityContainer.RegisterType(from, to, GetLifeTimeManager(lifetime), injectionMembers);
            return this;
        }

        public IContainer RegisterType(Type from, Type to, string name, Lifetime lifetime, params Injection[] injections)
        {
            var injectionMembers = GetInjectionParameters(injections);
            _unityContainer.RegisterType(from, to, name, GetLifeTimeManager(lifetime), injectionMembers);
            return this;
        }

        public IContainer RegisterType<TFrom, TTo>(Lifetime lifetime, params Injection[] injections) where TTo : TFrom
        {
            var injectionMembers = GetInjectionParameters(injections);
            _unityContainer.RegisterType<TFrom, TTo>(GetLifeTimeManager(lifetime), injectionMembers);
            return this;
        }
        public IContainer RegisterType<TFrom, TTo>(params Injection[] injections) where TTo : TFrom
        {
            var injectionMembers = GetInjectionParameters(injections);
            _unityContainer.RegisterType<TFrom, TTo>(injectionMembers);
            return this;
        }

        public IContainer RegisterType<TFrom, TTo>(string name, params Injection[] injections) where TTo : TFrom
        {
            var injectionMembers = GetInjectionParameters(injections);
            _unityContainer.RegisterType<TFrom, TTo>(name, injectionMembers);
            return this;
        }

        public IContainer RegisterType<TFrom, TTo>(string name, Lifetime lifetime, params Injection[] injections) where TTo : TFrom
        {
            var injectionMembers = GetInjectionParameters(injections);
            _unityContainer.RegisterType<TFrom, TTo>(name, GetLifeTimeManager(lifetime), injectionMembers);
            return this;
        }

        public object Resolve(Type t, params Parameter[] parameters)
        {
            return _unityContainer.Resolve(t, GetResolverOverrides(parameters));
        }

        public object Resolve(Type t, string name, params Parameter[] parameters)
        {
            return _unityContainer.Resolve(t, name, GetResolverOverrides(parameters));
        }

        public T Resolve<T>(params Parameter[] parameters)
        {
            return _unityContainer.Resolve<T>(GetResolverOverrides(parameters));
        }

        public T Resolve<T>(string name, params Parameter[] parameters)
        {
            return _unityContainer.Resolve<T>(name, GetResolverOverrides(parameters));
        }

        public IEnumerable<object> ResolveAll(Type type, params Parameter[] parameters)
        {
            return _unityContainer.ResolveAll(type, GetResolverOverrides(parameters));

        }

        public IEnumerable<T> ResolveAll<T>(params Parameter[] parameters)
        {
            return _unityContainer.ResolveAll<T>(GetResolverOverrides(parameters));
        }

        LifetimeManager GetLifeTimeManager(Lifetime lifetime)
        {
            LifetimeManager lifetimeManager = null;
            switch (lifetime)
            {
                case Lifetime.Singleton:
                    lifetimeManager = new ContainerControlledLifetimeManager();
                    break;
                case Lifetime.Hierarchical:
                    lifetimeManager = new HierarchicalLifetimeManager();
                    break;
                case Lifetime.PerRequest:
                    lifetimeManager = new PerRequestLifetimeManager();
                    break;
                case Lifetime.Transient:
                    lifetimeManager = new TransientLifetimeManager();
                    break;
            }

            if (lifetimeManager == null)
            {
                throw new Exception($"{lifetime.ToString()} is not supported.");
            }
            return lifetimeManager;
        }

        InjectionMember[] GetInjectionParameters(Injection[] injections)
        {
            var injectionMembers = new List<InjectionMember>();
            injections.ForEach(injection =>
            {
                if (injection is ConstructInjection)
                {
                    var constructInjection = injection as ConstructInjection;
                    injectionMembers.Add(new InjectionConstructor(constructInjection.Objects));
                }
                else if (injection is PropertyInjection)
                {
                    var propertyInjection = injection as PropertyInjection;
                    injectionMembers.Add(new InjectionProperty(propertyInjection.PropertyName, propertyInjection.PropertyValue));
                }
            });
            return injectionMembers.ToArray();
        }


        ResolverOverride[] GetResolverOverrides(Parameter[] parameters)
        {
            List<ResolverOverride> resolverOverrides = new List<ResolverOverride>();
            parameters.ForEach(parameter =>
            {
                resolverOverrides.Add(new ParameterOverride(parameter.Name, parameter.Value));
            });
            return resolverOverrides.ToArray();
        }
    }
}
