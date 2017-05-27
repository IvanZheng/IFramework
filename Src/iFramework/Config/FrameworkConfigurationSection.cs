using System.Configuration;

namespace IFramework.Config
{
    [ConfigurationSectionName("frameworkConfiguration")]
    public class FrameworkConfigurationSection : ConfigurationSection
    {
        /// <summary>
        ///     Gets or sets the configuration settings for handlers.
        /// </summary>
        [ConfigurationProperty("handlers", IsRequired = false)]
        public HandlerElementCollection Handlers
        {
            get => (HandlerElementCollection) base["handlers"];
            set => base["handlers"] = value;
        }

        [ConfigurationProperty("messageEndpointMappings", IsRequired = false)]
        public MessageEndpointElementCollection MessageEndpointMappings
        {
            get => (MessageEndpointElementCollection) base["messageEndpointMappings"];
            set => base["messageEndpointMappings"] = value;
        }
    }
}