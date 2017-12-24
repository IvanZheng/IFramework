using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Autofac;
using IFramework.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Autofac.Extensions.DependencyInjection;

namespace IFramework.DependencyInjection.Autofac
{
    public class ObjectProvider : IObjectProvider
    {
        private ILifetimeScope _scope;
        private IEnumerable<global::Autofac.Core.Parameter> GetResolvedParameters(Parameter[] resolvedParameters)
        {
            var parameters = new List<global::Autofac.Core.Parameter>();
            //parameters.Add(new NamedParameter("container",  this));
            parameters.AddRange(resolvedParameters.Select(p => new NamedParameter(p.Name, p.Value)));
            return parameters;
        }

        internal ObjectProvider(ObjectProvider parent = null)
        {
            Parent = parent;
        }


        internal void SetScope(ILifetimeScope scope)
        {
            _scope = scope;
        }
        public ObjectProvider(ILifetimeScope scope, ObjectProvider parent = null)
            : this(parent)
        {
            SetScope(scope);
        }

        public void Dispose()
        {
            _scope.Dispose();
        }

        public IObjectProvider Parent { get; }
        public IObjectProvider CreateScope()
        {
            var objectProvider = new ObjectProvider(this);
            var childScope = _scope.BeginLifetimeScope(builder =>
            {
                builder.RegisterInstance<IObjectProvider>(objectProvider);
            });
            objectProvider.SetScope(childScope);
            return objectProvider;
        }

        public IObjectProvider CreateScope(IServiceCollection serviceCollection)
        {
            var objectProvider = new ObjectProvider(this);
            var childScope = _scope.BeginLifetimeScope(builder =>
            {
                builder.RegisterInstance<IObjectProvider>(objectProvider);
                builder.Populate(serviceCollection);
            });
            objectProvider.SetScope(childScope);
            return objectProvider;

        }

        public IObjectProvider CreateScope(Action<IObjectProviderBuilder> buildAction)
        {
            if (buildAction == null)
            {
                throw new ArgumentNullException(nameof(buildAction));
            }
            var objectProvider = new ObjectProvider(this);
            var childScope = _scope.BeginLifetimeScope(builder =>
            {
                builder.RegisterInstance<IObjectProvider>(objectProvider);
                var providerBuilder = new ObjectProviderBuilder(builder);
                buildAction(providerBuilder);
            });
            objectProvider.SetScope(childScope);
            return objectProvider;
        }

        public object GetService(Type t, params Parameter[] parameters)
        {
            return _scope.Resolve(t, GetResolvedParameters(parameters));
        }

        public object GetService(Type t, string name, params Parameter[] parameters)
        {
            return _scope.ResolveNamed(name, t, GetResolvedParameters(parameters));
        }

        public T GetService<T>(params Parameter[] parameters)
        {
            return _scope.Resolve<T>(GetResolvedParameters(parameters));
        }

        public T GetService<T>(string name, params Parameter[] parameters)
        {
            return _scope.ResolveNamed<T>(name, GetResolvedParameters(parameters));
        }

        public IEnumerable<object> GetAllServices(Type type, params Parameter[] parameters)
        {
            var typeToResolve = typeof(IEnumerable<>).MakeGenericType(type);
            return _scope.Resolve(typeToResolve, GetResolvedParameters(parameters)) as IEnumerable<object>;
        }

        public IEnumerable<T> GetAllServices<T>(params Parameter[] parameters)
        {
            return _scope.Resolve<IEnumerable<T>>(GetResolvedParameters(parameters));
        }

        public object GetService(Type serviceType)
        {
            return _scope.Resolve(serviceType);
        }
    }
}
