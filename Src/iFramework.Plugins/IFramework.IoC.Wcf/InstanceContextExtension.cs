using System;
using System.ServiceModel;

namespace IFramework.IoC.Wcf
{
    public class InstanceContextExtension : IExtension<InstanceContext>
    {
        private IContainer _childContainer;

        public IContainer GetChildContainer(IContainer container)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            return _childContainer ?? (_childContainer = container.CreateChildContainer());
        }

        public void DisposeOfChildContainer()
        {
            _childContainer?.Dispose();
        }

        public void Attach(InstanceContext owner)
        {            
        }

        public void Detach(InstanceContext owner)
        {            
        }        
    }
}
