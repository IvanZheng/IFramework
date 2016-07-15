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

        public IContainer RegisterInstance(Type t, object instance)
        {
            _unityContainer.RegisterInstance(t, instance);
            return this;
        }

        public IContainer RegisterInstance(Type t, string name, object instance)
        {
            _unityContainer.RegisterInstance(t, name, instance);
            return this;
        }

        public IContainer RegisterInstance<TInterface>(TInterface instance)
        {
            _unityContainer.RegisterInstance(instance);
            return this;
        }

        public IContainer RegisterInstance<TInterface>(string name, TInterface instance)
        {
            _unityContainer.RegisterInstance<TInterface>(name, instance);
            return this;
        }
     

        public IContainer RegisterType(Type from, Type to, string name = null)
        {
            if (string.IsNullOrEmpty(name))
            {
                _unityContainer.RegisterType(from, to);
            }
            else
            {
                _unityContainer.RegisterType(from, to, name);
            }
            return this;
        }

        public IContainer RegisterType(Type from, Type to, Lifetime lifetime)
        {
            _unityContainer.RegisterType(from, to, GetLifeTimeManager(lifetime));
            return this;
        }

        public IContainer RegisterType(Type from, Type to, string name, Lifetime lifetime)
        {
            _unityContainer.RegisterType(from, to, name, GetLifeTimeManager(lifetime));
            return this;
        }

        public IContainer RegisterType<TFrom, TTo>(Lifetime lifetime) where TTo : TFrom
        {
            _unityContainer.RegisterType<TFrom, TTo>(GetLifeTimeManager(lifetime));
            return this;
        }

        public IContainer RegisterType<TFrom, TTo>(string name = null) where TTo : TFrom
        {
            _unityContainer.RegisterType<TFrom, TTo>(name);
            return this;
        }

        public IContainer RegisterType<TFrom, TTo>(string name, Lifetime lifetime) where TTo : TFrom
        {
            _unityContainer.RegisterType<TFrom, TTo>(name, GetLifeTimeManager(lifetime));
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

        ResolverOverride[] GetResolverOverrides(Parameter[]  parameters)
        {
            List<ResolverOverride> resolverOverrides = new List<ResolverOverride>();
            parameters.ForEach(parameter => {
            resolverOverrides.Add(new ParameterOverride(parameter.Name, parameter.Value));
            });
            return resolverOverrides.ToArray();
        }
    }
}
