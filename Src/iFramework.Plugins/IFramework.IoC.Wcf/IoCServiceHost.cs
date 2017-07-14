using System;
using System.ServiceModel;
using System.ServiceModel.Description;

namespace IFramework.IoC.Wcf
{
    public class IoCServiceHost : System.ServiceModel.ServiceHost
    {
        public IoCServiceHost(IContainer container, Type serviceType, params Uri[] baseAddresses)
            : base(serviceType, baseAddresses)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            ApplyServiceBehaviors(container);

            ApplyContractBehaviors(container);

            foreach (var contractDescription in ImplementedContracts.Values)
            {
                var contractBehavior =
                    new ContractBehavior(new InstanceProvider(container, contractDescription.ContractType));

                contractDescription.Behaviors.Add(contractBehavior);
            }
        }

        private void ApplyContractBehaviors(IContainer container)
        {
            var registeredContractBehaviors = container.ResolveAll<IContractBehavior>();

            foreach (var contractBehavior in registeredContractBehaviors)
            {
                foreach (var contractDescription in ImplementedContracts.Values)
                {
                    contractDescription.Behaviors.Add(contractBehavior);
                }                
            }
        }

        private void ApplyServiceBehaviors(IContainer container)
        {
            var registeredServiceBehaviors = container.ResolveAll<IServiceBehavior>();

            foreach (var serviceBehavior in registeredServiceBehaviors)
            {
                Description.Behaviors.Add(serviceBehavior);
            }
        }
    }
}