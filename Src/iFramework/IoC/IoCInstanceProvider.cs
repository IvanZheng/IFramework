using System;
using System.ServiceModel;
using System.ServiceModel.Dispatcher;

namespace IFramework.IoC
{
    public class IocInstanceProvider : IInstanceProvider
    {
        private readonly IContainer _container;
        private readonly Type _serviceType;

        public IocInstanceProvider(Type serviceType)
        {
            _serviceType = serviceType;
            _container = IoCFactory.Instance.CurrentContainer;
        }

        #region IInstanceProvider Members

        public object GetInstance(InstanceContext instanceContext, System.ServiceModel.Channels.Message message)
        {
            return _container.Resolve(_serviceType);
        }

        public object GetInstance(InstanceContext instanceContext)
        {
            return GetInstance(instanceContext, null);
        }

        public void ReleaseInstance(InstanceContext instanceContext, object instance)
        {
            if (instance is IDisposable)
                ((IDisposable) instance).Dispose();
        }

        #endregion
    }
}