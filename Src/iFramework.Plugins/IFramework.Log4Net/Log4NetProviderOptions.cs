namespace IFramework.Log4Net
{
    public class Log4NetProviderOptions
    {  
        public string ConfigFile { get; set; }
        public bool EnableScope { get; set; }
        /// <summary>
        /// Enable capture of properties from the ILogger-State-object, both in <see cref="Microsoft.Extensions.Logging.ILogger.Log"/> and <see cref="Microsoft.Extensions.Logging.ILogger.BeginScope"/>
        /// </summary>
        public bool CaptureMessageProperties { get; set; }

        /// <summary>
        /// Use the Log4Net engine for parsing the message template (again) and format using the Log4Net formatter
        /// </summary>
        public bool ParseMessageTemplates { get; set; }

        /// <summary>Initializes a new instance of the <see cref="T:System.Object" /> class.</summary>
        public Log4NetProviderOptions()
        {
            ConfigFile = "log4net.config";
            CaptureMessageProperties = true;
            ParseMessageTemplates = false;
            EnableScope = false;
        }

        /// <summary>
        /// Default options
        /// </summary>
        internal static Log4NetProviderOptions Default = new Log4NetProviderOptions();
    }
}
