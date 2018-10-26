using System.Configuration;

namespace IFramework.Config
{
    public class HandlerElement
    {
        /// <summary>
        /// Handler Element Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// SourceType 
        /// </summary>
        public HandlerSourceType SourceType { get; set; }

        /// <summary>
        /// source name path
        /// </summary>
        public string Source { get; set; }
    }
}