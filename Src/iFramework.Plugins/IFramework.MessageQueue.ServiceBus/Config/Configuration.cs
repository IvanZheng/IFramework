using IFramework.Config;
using IFramework.Infrastructure;
using System;

namespace IFramework.Config
{
    public static class IFrameworkConfigurationExtension
    {
        static string _MessageQueueNameFormat = string.Empty;
        static TimeSpan _ReceiveMessageTimeout = new TimeSpan(0, 0, 10); 

        public static TimeSpan GetMessageQueueReceiveMessageTimeout(this Configuration configuration)
        {
            return _ReceiveMessageTimeout;
        }

        public static Configuration SetMessageQueueReceiveMessageTimeout(this Configuration configuration, TimeSpan timeout)
        {
            _ReceiveMessageTimeout = timeout;
            return configuration;
        }
       
        public static Configuration SetMessageQueueNameFormat(this Configuration configuration, string format)
        {
            _MessageQueueNameFormat = format;
            return configuration;
        }

        public static Configuration MessageQueueUseMachineNameFormat(this Configuration configuration, bool onlyInDebug = true)
        {
            var compliationSection = Configuration.GetCompliationSection();
            if (!onlyInDebug || (compliationSection != null && compliationSection.Debug))
            {
                configuration.SetMessageQueueNameFormat(Environment.MachineName + ".{0}");
            }
            return configuration;
        }


        public static string FormatMessageQueueName(this Configuration configuration, string name)
        {
            return string.IsNullOrEmpty(_MessageQueueNameFormat) ?
                          name:
                          string.Format(_MessageQueueNameFormat, name);
        }

       
    }
}
