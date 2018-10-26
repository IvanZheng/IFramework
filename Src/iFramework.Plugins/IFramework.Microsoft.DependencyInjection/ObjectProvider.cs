using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace IFramework.DependencyInjection.Microsoft
{
    public class ObjectProvider : IObjectProvider
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IServiceScope _serviceScope;

        public ObjectProvider(IServiceProvider provider)
        {
            _serviceProvider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public ObjectProvider(IServiceCollection serviceCollection)
        {
            if (serviceCollection == null)
            {
                throw new ArgumentNullException(nameof(serviceCollection));
            }

            _serviceProvider = serviceCollection.BuildServiceProvider();
        }

        public ObjectProvider(IServiceScope serviceScope, IObjectProvider parent)
        {

            _serviceScope = serviceScope ?? throw new ArgumentNullException(nameof(serviceScope));
            _serviceProvider = serviceScope.ServiceProvider;
            Parent = parent;
        }

        public void Dispose()
        {
            _serviceScope?.Dispose();
        }

        public IObjectProvider Parent { get; }

        public IObjectProvider CreateScope()
        {
            return new ObjectProvider(_serviceProvider.CreateScope(), this);
        }

        public IObjectProvider CreateScope(IServiceCollection serviceCollection)
        {
            throw new NotImplementedException();
        }

        public IObjectProvider CreateScope(Action<IObjectProviderBuilder> buildAction)
        {
            throw new NotImplementedException();
        }

        public object GetService(Type serviceType)
        {
            return _serviceProvider.GetService(serviceType);
        }

        public object GetService(Type t, string name, params Parameter[] parameters)
        {
            throw new NotImplementedException();
        }

        public object GetService(Type t, params Parameter[] parameters)
        {
            if (parameters.Length > 0)
            {
                throw new NotImplementedException();
            }
            return GetService(t);
        }

        public T GetService<T>(params Parameter[] overrides) where T : class
        {
            if (overrides.Length > 0)
            {
                throw new NotImplementedException();
            }
            return (T) GetService(typeof(T));
        }

        public T GetService<T>(string name, params Parameter[] overrides) where T : class
        {
            throw new NotImplementedException();
        }

        public IEnumerable<object> GetAllServices(Type type, params Parameter[] parameters)
        {
            if (parameters.Length > 0)
            {
                throw new NotImplementedException();
            }
            return _serviceProvider.GetServices(type);
        }

        public IEnumerable<T> GetAllServices<T>(params Parameter[] parameters) where T : class
        {
            if (parameters.Length > 0)
            {
                throw new NotImplementedException();
            }
            return GetAllServices(typeof(T)).Cast<T>();
        }

        public object GetRequiredService(Type serviceType)
        {
            return _serviceProvider.GetRequiredService(serviceType);
        }
    }
}