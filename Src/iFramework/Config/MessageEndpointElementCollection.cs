using System.Configuration;

namespace IFramework.Config
{
    [ConfigurationCollection(typeof(EndpointElement), AddItemName = "endpoint",
        CollectionType = ConfigurationElementCollectionType.BasicMap)]
    public class MessageEndpointElementCollection : BaseConfigurationElementCollection<EndpointElement>
    {
    }
}