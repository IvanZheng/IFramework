using System;
using System.Collections.Generic;
using System.Web.Http.Dependencies;

namespace IFramework.IoC.WebApi
{
    //using Microsoft.Practices.Unity;

    public sealed class HierarchicalDependencyResolver : IDependencyResolver, IDependencyScope, IDisposable
    {
        private readonly IContainer container;

        public HierarchicalDependencyResolver(IContainer container)
        {
            if (container == null)
            {
                throw new ArgumentNullException("container");
            }
            this.container = container.CreateChildContainer();
        }

        public IDependencyScope BeginScope()
        {
            return new UnityHierarchicalDependencyScope(container);
        }

        public void Dispose()
        {
            container.Dispose();
        }

        public object GetService(Type serviceType)
        {
            try
            {
                return container.Resolve(serviceType);
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
                return container.ResolveAll(serviceType);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void RegisterComponents(Action<IContainer> registerComponets)
        {
            registerComponets(container);
        }

        private sealed class UnityHierarchicalDependencyScope : IDependencyScope, IDisposable
        {
            private readonly IContainer container;

            public UnityHierarchicalDependencyScope(IContainer parentContainer)
            {
                container = parentContainer.CreateChildContainer();
            }

            public void Dispose()
            {
                container.Dispose();
            }

            public object GetService(Type serviceType)
            {
                return container.Resolve(serviceType);
            }

            public IEnumerable<object> GetServices(Type serviceType)
            {
                return container.ResolveAll(serviceType);
            }
        }
    }
}