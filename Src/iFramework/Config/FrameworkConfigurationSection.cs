using System.Configuration;

namespace IFramework.Config
{
    public class FrameworkConfigurationSection
    {
        /// <summary>
        ///     Gets or sets the configuration settings for handlers.
        /// </summary>
        public HandlerElementCollection Handlers { get; set; }

        public MessageEndpointElementCollection MessageEndpointMappings { get; set; }
    }
}