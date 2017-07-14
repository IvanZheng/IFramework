using System;
using System.ServiceModel;
using System.ServiceModel.Activation;

namespace IFramework.IoC.Wcf
{
    public abstract class IoCServiceHostFactory : ServiceHostFactory
    {
        private readonly IContainer _container;

        protected IoCServiceHostFactory(IContainer container)
        {
            _container = container;
        }

        protected virtual void ConfigureContainer(IContainer container) { }


        protected override ServiceHost CreateServiceHost(Type serviceType, Uri[] baseAddresses)
        {
            ConfigureContainer(_container);
            return new IoCServiceHost(_container, serviceType, baseAddresses);
        }
    }
}