using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;

namespace IFramework.IoC.Wcf
{
    public class InstanceContextInitializer : IInstanceContextInitializer
    {
        public void Initialize(InstanceContext instanceContext,
                               System.ServiceModel.Channels.Message message)
        {
            instanceContext.Extensions.Add(new InstanceContextExtension());
        }
    }
}
