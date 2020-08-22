using System;
using System.Collections.Generic;
using IFramework.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Unity;
using Unity.Resolution;

namespace IFramework.DependencyInjection.Unity
{
    public class ObjectProvider : ObjectProviderBase
    {
        public ObjectProvider(ObjectProvider parent = null)
        {
            Parent = parent;
        }

        public ObjectProvider(IUnityContainer container, ObjectProvider parent = null)
            : this(parent)
        {
            SetComponentContext(container);
        }

        public IUnityContainer UnityContainer { get; private set; }

        public override IObjectProvider Parent { get; }

        public override object GetService(Type serviceType)
        {
            return UnityContainer.Resolve(serviceType);
        }

        /// <summary>
        /// </summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        /// <exception cref="T:System.InvalidOperationException">There is no service of type <typeparamref name="T" />.</exception>
        public override object GetRequiredService(Type serviceType)
        {
            return UnityContainer.Resolve(serviceType) ?? throw new InvalidOperationException($"There is no service of type {serviceType.Name}");
        }

        public override void Dispose()
        {
            UnityContainer.Dispose();
        }

        public override IObjectProvider CreateScope()
        {
            return CreateScope(new ServiceCollection());
        }

        public override IObjectProvider CreateScope(IServiceCollection serviceCollection)
        {
            var objectProvider = new ObjectProvider(this);
            var childScope = UnityContainer.CreateChildContainer();
            var builder = new ObjectProviderBuilder(childScope);
            builder.Register(context => context, ServiceLifetime.Scoped);
            builder.Register<IServiceProvider>(context => context, ServiceLifetime.Scoped);
            builder.Populate(serviceCollection);
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
            var childScope = UnityContainer.CreateChildContainer();
            var providerBuilder = new ObjectProviderBuilder(childScope);
            providerBuilder.Register(context => context, ServiceLifetime.Scoped);
            providerBuilder.Register<IServiceProvider>(context => context, ServiceLifetime.Scoped);
            buildAction(providerBuilder);
            objectProvider.SetComponentContext(childScope);
            return objectProvider;
        }

        public override object GetService(Type t, string name, params Parameter[] parameters)
        {
            return UnityContainer.Resolve(t, name, GetResolverOverrides(parameters));
        }

        public override object GetService(Type t, params Parameter[] parameters)
        {
            return UnityContainer.Resolve(t, GetResolverOverrides(parameters));
        }

        public override T GetService<T>(params Parameter[] parameters)
        {
            return UnityContainer.Resolve<T>(GetResolverOverrides(parameters));
        }

        public override T GetService<T>(string name, params Parameter[] parameters)
        {
            return UnityContainer.Resolve<T>(name, GetResolverOverrides(parameters));
        }

        public override IEnumerable<object> GetAllServices(Type type, params Parameter[] parameters)
        {
            return UnityContainer.ResolveAll(type, GetResolverOverrides(parameters));
        }

        public override IEnumerable<T> GetAllServices<T>(params Parameter[] parameters)
        {
            return UnityContainer.ResolveAll<T>(GetResolverOverrides(parameters));
        }

        internal void SetComponentContext(IUnityContainer container)
        {
            UnityContainer = container;
        }

        private ResolverOverride[] GetResolverOverrides(Parameter[] parameters)
        {
            var resolverOverrides = new List<ResolverOverride>();
            parameters.ForEach(parameter => { resolverOverrides.Add(new ParameterOverride(parameter.Name, parameter.Value)); });
            return resolverOverrides.ToArray();
        }
    }
}