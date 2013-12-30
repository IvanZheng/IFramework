using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace IFramework.Config
{
    [ConfigurationCollection(typeof(EndpointElement), AddItemName = "endpoint", CollectionType = ConfigurationElementCollectionType.BasicMap)]
    public class MessageEndpointElementCollection : BaseConfigurationElementCollection<EndpointElement>
    {

    }
}
