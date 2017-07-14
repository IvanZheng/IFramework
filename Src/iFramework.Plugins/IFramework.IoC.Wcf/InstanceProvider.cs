using System;
using System.ServiceModel;
using System.ServiceModel.Dispatcher;

namespace IFramework.IoC.Wcf
{
    public class InstanceProvider : IInstanceProvider
    {
        private readonly IContainer _container;
        private readonly Type _contractType;
        
        public InstanceProvider(IContainer container, Type contractType)
        {
            if (contractType == null)
            {
                throw new ArgumentNullException(nameof(contractType));
            }

            _container = container ?? throw new ArgumentNullException(nameof(container));
            _contractType = contractType;
        }

        public object GetInstance(InstanceContext instanceContext, System.ServiceModel.Channels.Message message)
        {
            var childContainer =
                instanceContext.Extensions.Find<InstanceContextExtension>().GetChildContainer(_container);

            return childContainer.Resolve(_contractType);
        }

        public object GetInstance(InstanceContext instanceContext)
        {
            return GetInstance(instanceContext, null);
        }

        public void ReleaseInstance(InstanceContext instanceContext, object instance)
        {
            instanceContext.Extensions.Find<InstanceContextExtension>().DisposeOfChildContainer();            
        }        
    }
}