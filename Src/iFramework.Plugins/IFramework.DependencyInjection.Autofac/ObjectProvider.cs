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
    public class ObjectProvider : ObjectProviderBase
    {
        private IComponentContext _componentContext;
        public ILifetimeScope Scope => _componentContext as ILifetimeScope;
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

        public override void Dispose()
        {
            Scope?.Dispose();
            _componentContext = null;
            Parent = null;
        }

        public override IObjectProvider CreateScope()
        {
            var objectProvider = new ObjectProvider(this);
            var childScope = Scope.BeginLifetimeScope(builder =>
            {
                builder.RegisterInstance<IObjectProvider>(objectProvider);
            });
            objectProvider.SetComponentContext(childScope);
            return objectProvider;
        }

        public override IObjectProvider CreateScope(IServiceCollection serviceCollection)
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

        public override IObjectProvider CreateScope(Action<IObjectProviderBuilder> buildAction)
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

        public override object GetService(Type t, params Parameter[] parameters)
        {
            return _componentContext.ResolveOptional(t, GetResolvedParameters(parameters));
        }


        public override T GetService<T>(params Parameter[] parameters)
        {
            return ResolutionExtensions.ResolveOptional<T>(_componentContext, GetResolvedParameters(parameters));
        }
        public override object GetService(Type t, string name, params Parameter[] parameters)
        {
            return _componentContext.ResolveNamed(name, t, GetResolvedParameters(parameters));
        }

        public override T GetService<T>(string name, params Parameter[] parameters)
        {
            return ResolutionExtensions.ResolveOptionalNamed<T>(_componentContext, name, GetResolvedParameters(parameters));
        }

        public  override IEnumerable<object> GetAllServices(Type type, params Parameter[] parameters)
        {
            var typeToResolve = typeof(IEnumerable<>).MakeGenericType(type);
            return _componentContext.ResolveOptional(typeToResolve, GetResolvedParameters(parameters)) as IEnumerable<object>;
        }

        public  override IEnumerable<T> GetAllServices<T>(params Parameter[] parameters)
        {
            return ResolutionExtensions.ResolveOptional<IEnumerable<T>>(_componentContext, GetResolvedParameters(parameters));
        }

        public  override object GetService(Type serviceType)
        {
            return _componentContext.ResolveOptional(serviceType);
        }

        public override object GetRequiredService(Type serviceType)
        {
            return _componentContext.Resolve(serviceType);
        }
    }
}
