using IFramework.Infrastructure;
using Autofac;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac.Core;

namespace IFramework.Autofac
{
    public static class ObjectContainerExtension
    {
        public static ILifetimeScope GetAutofacContainer(this IoC.IContainer container)
        {
            return (container as ObjectContainer)?._container;
        }
    }

    public class ObjectContainer : IoC.IContainer
    {
        readonly string AutofacNotSupportedException = "autofac doesn't support retrieve parent container";
        internal ILifetimeScope _container;
        public ObjectContainer(ILifetimeScope container)
        {
            _container = container;
        }

        public object ContainerInstanse
        {
            get
            {
                return _container;
            }
        }

        public IoC.IContainer Parent
        {
            get
            {
                throw new NotSupportedException(AutofacNotSupportedException);
            }
        }

        public IoC.IContainer CreateChildContainer()
        {
            return new ObjectContainer(_container.BeginLifetimeScope());
        }

        public void Dispose()
        {
            _container.Dispose();
        }

        public IoC.IContainer RegisterInstance(Type t, object instance, IoC.Lifetime lifetime = IoC.Lifetime.Singleton)
        {
            var builder = new ContainerBuilder();
            builder.RegisterInstance(instance)
                .As(t)
                .InstanceLifetime(lifetime);
            builder.Update(_container.ComponentRegistry);
            return this;
        }

        public IoC.IContainer RegisterInstance(Type t, string name, object instance, IoC.Lifetime lifetime = IoC.Lifetime.Singleton)
        {
            var builder = new ContainerBuilder();
            builder.RegisterInstance(instance)
                .Named(name, t)
                .InstanceLifetime(lifetime);
            builder.Update(_container.ComponentRegistry);
            return this;
        }

        public IoC.IContainer RegisterInstance<TInterface>(TInterface instance, IoC.Lifetime lifetime = IoC.Lifetime.Singleton)
            where TInterface : class
        {
            var builder = new ContainerBuilder();
            builder.RegisterInstance(instance)
                .InstanceLifetime(lifetime);
            builder.Update(_container.ComponentRegistry);
            return this;
        }

        public IoC.IContainer RegisterInstance<TInterface>(string name, TInterface instance, IoC.Lifetime lifetime = IoC.Lifetime.Singleton)
             where TInterface : class
        {
            var builder = new ContainerBuilder();
            builder.RegisterInstance(instance)
                .Named<TInterface>(name)
                .InstanceLifetime(lifetime);
            builder.Update(_container.ComponentRegistry);
            return this;
        }


        public IoC.IContainer RegisterType(Type from, Type to, string name = null, params IoC.Injection[] injections)
        {
            var injectionMembers = GetInjectionParameters(injections);
            var builder = new ContainerBuilder();

            if (string.IsNullOrEmpty(name))
            {
                builder.RegisterType(to).As(from);
            }
            else
            {
                builder.RegisterType(to)
                    .Named(name, from)
                    .WithParameters(injectionMembers);

            }
            builder.Update(_container.ComponentRegistry);
            return this;
        }

        public IoC.IContainer RegisterType(Type from, Type to, IoC.Lifetime lifetime, params IoC.Injection[] injections)
        {
            var injectionMembers = GetInjectionParameters(injections);
            var builder = new ContainerBuilder();
            builder.RegisterType(to)
                .As(from)
                .WithParameters(injectionMembers)
                .InstanceLifetime(lifetime);
            builder.Update(_container.ComponentRegistry);
            return this;
        }

        public IoC.IContainer RegisterType(Type from, Type to, string name, IoC.Lifetime lifetime, params IoC.Injection[] injections)
        {
            var injectionMembers = GetInjectionParameters(injections);
            var builder = new ContainerBuilder();
            builder.RegisterType(to)
                .Named(name, from)
                .InstanceLifetime(lifetime)
                .WithParameters(injectionMembers);
            builder.Update(_container.ComponentRegistry);
            return this;
        }

        public IoC.IContainer RegisterType<TFrom, TTo>(IoC.Lifetime lifetime, params IoC.Injection[] injections) where TTo : TFrom
        {
            var injectionMembers = GetInjectionParameters(injections);
            var builder = new ContainerBuilder();
            builder.RegisterType(typeof(TTo))
                .As(typeof(TFrom))
                .InstanceLifetime(lifetime)
                .WithParameters(injectionMembers);
            builder.Update(_container.ComponentRegistry);
            return this;
        }
        public IoC.IContainer RegisterType<TFrom, TTo>(params IoC.Injection[] injections) where TTo : TFrom
        {
            var injectionMembers = GetInjectionParameters(injections);
            var builder = new ContainerBuilder();
            builder.RegisterType(typeof(TTo))
                .As(typeof(TFrom))
                .WithParameters(injectionMembers);
            builder.Update(_container.ComponentRegistry);
            return this;
        }

        public IoC.IContainer RegisterType<TFrom, TTo>(string name, params IoC.Injection[] injections) where TTo : TFrom
        {
            var injectionMembers = GetInjectionParameters(injections);
            var builder = new ContainerBuilder();
            builder.RegisterType(typeof(TTo))
                .Named(name, typeof(TFrom))
                .WithParameters(injectionMembers);
            builder.Update(_container.ComponentRegistry);
            return this;
        }

        public IoC.IContainer RegisterType<TFrom, TTo>(string name, IoC.Lifetime lifetime, params IoC.Injection[] injections) where TTo : TFrom
        {
            var injectionMembers = GetInjectionParameters(injections);
            var builder = new ContainerBuilder();
            builder.RegisterType(typeof(TTo))
                   .Named(name, typeof(TFrom))
                   .InstanceLifetime(lifetime)
                   .WithParameters(injectionMembers);
            builder.Update(_container.ComponentRegistry);
            return this;
        }

        public object Resolve(Type t, params IoC.Parameter[] parameters)
        {
            return _container.Resolve(t, GetResolvedParameters(parameters));
        }

        public object Resolve(Type t, string name, params IoC.Parameter[] parameters)
        {
            return _container.ResolveNamed(name, t, GetResolvedParameters(parameters));
        }

        public T Resolve<T>(params IoC.Parameter[] parameters)
        {
            return _container.Resolve<T>(GetResolvedParameters(parameters));
        }

        public T Resolve<T>(string name, params IoC.Parameter[] parameters)
        {
            return _container.ResolveNamed<T>(name, GetResolvedParameters(parameters));
        }

        public IEnumerable<object> ResolveAll(Type type, params IoC.Parameter[] parameters)
        {
            throw new NotSupportedException(AutofacNotSupportedException);
        }

        public IEnumerable<T> ResolveAll<T>(params IoC.Parameter[] parameters)
        {
            throw new NotSupportedException(AutofacNotSupportedException);
        }


        IEnumerable<Parameter> GetResolvedParameters(IoC.Parameter[] resolvedParameters)
        {
            var parameters = new List<Parameter>();
            parameters.AddRange(resolvedParameters.Select(p => new NamedParameter(p.Name, p.Value)));
            return parameters;
        }

        IEnumerable<Parameter> GetInjectionParameters(IoC.Injection[] injections)
        {
            var injectionMembers = new List<Parameter>();
            injections.ForEach(injection =>
            {
                if (injection is IoC.ConstructInjection)
                {
                    var constructInjection = injection as IoC.ConstructInjection;
                    injectionMembers.AddRange(constructInjection.Parameters
                                                                .Select(p => new NamedParameter(p.ParameterName, p.ParameterValue)));
                }
                else if (injection is IoC.ParameterInjection)
                {
                    var propertyInjection = injection as IoC.ParameterInjection;
                    injectionMembers.Add(new NamedPropertyParameter(propertyInjection.ParameterName, propertyInjection.ParameterValue));
                }
            });
            return injectionMembers;
        }

    }
}
