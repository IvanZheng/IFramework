using IFramework.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http.Dependencies;

namespace Sample.CommandService.App_Start
{
    public sealed class HierarchicalDependencyResolver : IDependencyResolver, IDependencyScope, IDisposable
    {
        private readonly IObjectProvider _objectProvider;

        public HierarchicalDependencyResolver(IObjectProvider objectProvider)
        {
            if (objectProvider == null)
            {
                throw new ArgumentNullException(nameof(objectProvider));
            }
            this._objectProvider = objectProvider.CreateScope();
        }

        public IDependencyScope BeginScope()
        {
            return new HierarchicalDependencyScope(_objectProvider);
        }

        public void Dispose()
        {
            _objectProvider.Dispose();
        }

        public object GetService(Type serviceType)
        {
            try
            {
                return _objectProvider.GetService(serviceType);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            try
            {
                return _objectProvider.GetAllServices(serviceType);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void RegisterComponents(Action<IObjectProvider> registerComponets)
        {
            registerComponets(_objectProvider);
        }

        private sealed class HierarchicalDependencyScope : IDependencyScope, IDisposable
        {
            private readonly IObjectProvider _objectProvider;

            public HierarchicalDependencyScope(IObjectProvider parentObjectProvider)
            {
                _objectProvider = parentObjectProvider.CreateScope();
            }

            public void Dispose()
            {
                _objectProvider.Dispose();
            }

            public object GetService(Type serviceType)
            {
                return _objectProvider.GetService(serviceType);
            }

            public IEnumerable<object> GetServices(Type serviceType)
            {
                return _objectProvider.GetAllServices(serviceType);
            }
        }
    }
}