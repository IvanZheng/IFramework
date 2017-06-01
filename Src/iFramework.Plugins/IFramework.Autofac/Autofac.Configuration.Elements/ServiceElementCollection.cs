namespace Autofac.Configuration.Elements
{
    public class ServiceElementCollection : NamedConfigurationElementCollection<ServiceElement>
    {
        public ServiceElementCollection() : base("service", "type") { }
    }
}