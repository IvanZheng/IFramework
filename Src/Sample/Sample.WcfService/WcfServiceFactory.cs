using IFramework.IoC.Wcf;

namespace Sample.WcfService
{
    public class WcfServiceFactory: IoCServiceHostFactory
    {
        public WcfServiceFactory()
            : base(IoCConfig.GetConfiguredContainer())
        {
            
        }
    }
}