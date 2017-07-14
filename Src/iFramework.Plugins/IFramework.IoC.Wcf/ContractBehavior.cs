using System;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace IFramework.IoC.Wcf
{
    public class ContractBehavior : IContractBehavior
    {
        private readonly IInstanceProvider _instanceProvider;

        public ContractBehavior(IInstanceProvider instanceProvider)
        {
            _instanceProvider = instanceProvider ?? throw new ArgumentNullException(nameof(instanceProvider));
        }

        public void AddBindingParameters(ContractDescription contractDescription, ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyClientBehavior(ContractDescription contractDescription, ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
        }

        public void ApplyDispatchBehavior(ContractDescription contractDescription, ServiceEndpoint endpoint, DispatchRuntime dispatchRuntime)
        {
            dispatchRuntime.InstanceProvider = _instanceProvider;
            dispatchRuntime.InstanceContextInitializers.Add(new InstanceContextInitializer());
        }

        public void Validate(ContractDescription contractDescription, ServiceEndpoint endpoint)
        {
        }
    }
}