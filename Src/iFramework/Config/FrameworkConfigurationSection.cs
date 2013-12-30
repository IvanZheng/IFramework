using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace IFramework.Config
{
    [ConfigurationSectionName("frameworkConfiguration")]
    public class FrameworkConfigurationSection : ConfigurationSection
    {
        /// <summary>
        /// Gets or sets the configuration settings for handlers.
        /// </summary>
        [ConfigurationProperty("handlers", IsRequired = false)]
        public HandlerElementCollection Handlers
        {
            get { return (HandlerElementCollection)base["handlers"]; }
            set { base["handlers"] = value; }
        }

        [ConfigurationProperty("messageEndpointMappings", IsRequired=false)]
        public MessageEndpointElementCollection MessageEndpointMappings
        {
            get { return (MessageEndpointElementCollection)base["messageEndpointMappings"];}
            set { base["messageEndpointMappings"] = value;}
        }
    }
}
