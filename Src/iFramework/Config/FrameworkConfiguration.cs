using System.Configuration;

namespace IFramework.Config
{
    public class FrameworkConfiguration
    {
        /// <summary>
        ///     Gets or sets the configuration settings for handlers.
        /// </summary>
        public HandlerElement[] Handlers { get; set; }
    }
}