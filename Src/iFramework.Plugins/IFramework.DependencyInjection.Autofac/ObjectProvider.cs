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
        private readonly ILifetimeScope _scope;
        private bool _disposed;
        private IEnumerable<global::Autofac.Core.Parameter> GetResolvedParameters(Parameter[] resolvedParameters)
        {
            var parameters = new List<global::Autofac.Core.Parameter>();
            //parameters.Add(new NamedParameter("container",  this));
            parameters.AddRange(resolvedParameters.Select(p => new NamedParameter(p.Name, p.Value)));
            return parameters;
        }

        public ObjectProvider(ILifetimeScope scope, ObjectProvider parent = null)
        {
            _scope = scope;
            Parent = parent;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _scope.Dispose();
            }
        }

        public IObjectProvider Parent { get; }
        public IObjectProvider CreateScope()
        {
            IObjectProvider objectProvider = null;
            objectProvider = new ObjectProvider(_scope.BeginLifetimeScope(builder =>
            {
                builder.Register(componentContext => objectProvider)
                       .SingleInstance();
            }), this);
            return objectProvider;
        }

        public IObjectProvider CreateScope(IServiceCollection serviceCollection)
        {
            IObjectProvider objectProvider = null;
            objectProvider = new ObjectProvider(_scope.BeginLifetimeScope(builder =>
            {
                builder.Populate(serviceCollection);
                builder.Register(componentContext => objectProvider)
                       .SingleInstance();
            }), this);
            return objectProvider;
        }

        public IObjectProvider CreateScope(Action<IObjectProviderBuilder> buildAction)
        {
            if (buildAction == null)
            {
                throw new ArgumentNullException(nameof(buildAction));
            }
            IObjectProvider objectProvider = null;
            objectProvider = new ObjectProvider(_scope.BeginLifetimeScope(builder =>
            {
                builder.Register(componentContext => objectProvider)
                       .SingleInstance();
                var providerBuilder = new ObjectProviderBuilder(builder);
                buildAction(providerBuilder);
            }), this);
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
