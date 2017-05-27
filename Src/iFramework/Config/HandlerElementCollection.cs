using System.Configuration;

namespace IFramework.Config
{
    [ConfigurationCollection(typeof(HandlerElement), AddItemName = "handler",
        CollectionType = ConfigurationElementCollectionType.BasicMap)]
    public class HandlerElementCollection : BaseConfigurationElementCollection<HandlerElement>
    {
    }
}