using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace IFramework.DependencyInjection.Microsoft
{
    public class ObjectProvider : ObjectProviderBase
    {
        private readonly IServiceProvider _serviceProvider;
        private IServiceScope _serviceScope;

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

        public override  void Dispose()
        {
            _serviceScope?.Dispose();
            _serviceScope = null;
        }

        public override  IObjectProvider CreateScope()
        {
            return new ObjectProvider(_serviceProvider.CreateScope(), this);
        }

        public override  IObjectProvider CreateScope(IServiceCollection serviceCollection)
        {
            throw new NotImplementedException();
        }

        public override  IObjectProvider CreateScope(Action<IObjectProviderBuilder> buildAction)
        {
            throw new NotImplementedException();
        }

        public override  object GetService(Type serviceType)
        {
            return _serviceProvider.GetService(serviceType);
        }

        public override  object GetService(Type t, string name, params Parameter[] parameters)
        {
            throw new NotImplementedException();
        }

        public override  object GetService(Type t, params Parameter[] parameters)
        {
            if (parameters.Length > 0)
            {
                throw new NotImplementedException();
            }
            return GetService(t);
        }

        public override  T GetService<T>(params Parameter[] overrides)
        {
            if (overrides.Length > 0)
            {
                throw new NotImplementedException();
            }
            return (T) GetService(typeof(T));
        }

        public override  T GetService<T>(string name, params Parameter[] overrides)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<object> GetAllServices(Type type, params Parameter[] parameters)
        {
            if (parameters.Length > 0)
            {
                throw new NotImplementedException();
            }
            return _serviceProvider.GetServices(type);
        }

        public override IEnumerable<T> GetAllServices<T>(params Parameter[] parameters)
        {
            if (parameters.Length > 0)
            {
                throw new NotImplementedException();
            }
            return GetAllServices(typeof(T), default(Parameter[])).Cast<T>();
        }

        public  override object GetRequiredService(Type serviceType)
        {
            return _serviceProvider.GetRequiredService(serviceType);
        }
    }
}