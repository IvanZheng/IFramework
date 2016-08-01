
namespace IFramework.IoC.WebApi
{
    using IFramework.IoC;
    //using Microsoft.Practices.Unity;
    using System;
    using System.Collections.Generic;
    using System.Web.Http.Dependencies;

    public sealed class HierarchicalDependencyResolver : IDependencyResolver, IDependencyScope, IDisposable
    {
        private IContainer container;

        public HierarchicalDependencyResolver(IContainer container)
        {
            if (container == null)
            {
                throw new ArgumentNullException("container");
            }
            this.container = container.CreateChildContainer();
        }

        public void RegisterComponents(Action<IContainer> registerComponets)
        {
            registerComponets(container);
        }

        public IDependencyScope BeginScope()
        {
            return new UnityHierarchicalDependencyScope(this.container);
        }

        public void Dispose()
        {
            this.container.Dispose();
        }

        public object GetService(Type serviceType)
        {
            try
            {
                return this.container.Resolve(serviceType);
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
                return this.container.ResolveAll(serviceType);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private sealed class UnityHierarchicalDependencyScope : IDependencyScope, IDisposable
        {
            private IContainer container;

            public UnityHierarchicalDependencyScope(IContainer parentContainer)
            {
                this.container = parentContainer.CreateChildContainer();
            }

            public void Dispose()
            {
                this.container.Dispose();
            }

            public object GetService(Type serviceType)
            {
                return this.container.Resolve(serviceType);
            }

            public IEnumerable<object> GetServices(Type serviceType)
            {
                return this.container.ResolveAll(serviceType);
            }
        }
    }
}
