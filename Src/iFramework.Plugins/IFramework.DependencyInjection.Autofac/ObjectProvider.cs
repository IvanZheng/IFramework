using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Autofac;
using IFramework.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Autofac.Extensions.DependencyInjection;
using IFramework.Infrastructure;

namespace IFramework.DependencyInjection.Autofac
{
    public class ObjectProvider : IObjectProvider
    {
        private IComponentContext _componentContext;
        private ILifetimeScope Scope => _componentContext as ILifetimeScope;
        private IEnumerable<global::Autofac.Core.Parameter> GetResolvedParameters(Parameter[] resolvedParameters)
        {
            var parameters = new List<global::Autofac.Core.Parameter>();
            parameters.AddRange(resolvedParameters.Select(p => new NamedParameter(p.Name, p.Value)));
            return parameters;
        }

        internal ObjectProvider(ObjectProvider parent = null)
        {
            Parent = parent;
        }


        internal void SetComponentContext(IComponentContext componentContext)
        {
            _componentContext = componentContext;
        }
        public ObjectProvider(IComponentContext componentContext, ObjectProvider parent = null)
            : this(parent)
        {
            SetComponentContext(componentContext);
        }

        public void Dispose()
        {
            Scope?.Dispose();
        }

        public IObjectProvider Parent { get; }
        public IObjectProvider CreateScope()
        {
            var objectProvider = new ObjectProvider(this);
            var childScope = Scope.BeginLifetimeScope(builder =>
            {
                builder.RegisterInstance<IObjectProvider>(objectProvider);
            });
            objectProvider.SetComponentContext(childScope);
            return objectProvider;
        }

        public IObjectProvider CreateScope(IServiceCollection serviceCollection)
        {
            var objectProvider = new ObjectProvider(this);
            var childScope = Scope.BeginLifetimeScope(builder =>
            {
                builder.RegisterInstance<IObjectProvider>(objectProvider);
                builder.Populate(serviceCollection);
            });
            objectProvider.SetComponentContext(childScope);
            return objectProvider;

        }

        public IObjectProvider CreateScope(Action<IObjectProviderBuilder> buildAction)
        {
            if (buildAction == null)
            {
                throw new ArgumentNullException(nameof(buildAction));
            }
            var objectProvider = new ObjectProvider(this);
            var childScope = Scope.BeginLifetimeScope(builder =>
            {
                builder.RegisterInstance<IObjectProvider>(objectProvider);
                var providerBuilder = new ObjectProviderBuilder(builder);
                buildAction(providerBuilder);
            });
            objectProvider.SetComponentContext(childScope);
            return objectProvider;
        }

        public object GetService(Type t, params Parameter[] parameters)
        {
            return _componentContext.ResolveOptional(t, GetResolvedParameters(parameters));
        }


        public T GetService<T>(params Parameter[] parameters) where T : class
        {
            return _componentContext.ResolveOptional<T>(GetResolvedParameters(parameters));
        }
        public object GetService(Type t, string name, params Parameter[] parameters)
        {
            return _componentContext.ResolveNamed(name, t, GetResolvedParameters(parameters));
        }

        public T GetService<T>(string name, params Parameter[] parameters)
            where T : class
        {
            return _componentContext.ResolveOptionalNamed<T>(name, GetResolvedParameters(parameters));
        }

        public IEnumerable<object> GetAllServices(Type type, params Parameter[] parameters)
        {
            var typeToResolve = typeof(IEnumerable<>).MakeGenericType(type);
            return _componentContext.ResolveOptional(typeToResolve, GetResolvedParameters(parameters)) as IEnumerable<object>;
        }

        public IEnumerable<T> GetAllServices<T>(params Parameter[] parameters) where T : class
        {
            return _componentContext.ResolveOptional<IEnumerable<T>>(GetResolvedParameters(parameters));
        }

        public object GetService(Type serviceType)
        {
            return _componentContext.ResolveOptional(serviceType);
        }

        public object GetRequiredService(Type serviceType)
        {
            return _componentContext.Resolve(serviceType);
        }
    }
}
