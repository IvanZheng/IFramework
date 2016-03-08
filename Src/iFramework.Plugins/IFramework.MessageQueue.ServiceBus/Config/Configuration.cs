using IFramework.Config;
using IFramework.Infrastructure;
using Microsoft.Practices.Unity;
using System;

namespace IFramework.Config
{
    public static class IFrameworkConfigurationExtension
    {
        static string MessageQueueNameFormat { get; set; }
       
        public static Configuration SetMessageQueueNameFormat(this Configuration configuration, string format)
        {
            MessageQueueNameFormat = format;
            return configuration;
        }

        public static Configuration MessageQueueUseMachineNameFormat(this Configuration configuration, bool onlyInDebug = true)
        {
            var compliationSection = Configuration.GetCompliationSection();
            if (!onlyInDebug || (compliationSection != null && compliationSection.Debug))
            {
                MessageQueueNameFormat = Environment.MachineName + ".{0}";
            }
            return configuration;
        }


        public static string FormatMessageQueueName(this Configuration configuration, string name)
        {
            return string.IsNullOrEmpty(MessageQueueNameFormat) ?
                          name:
                          string.Format(MessageQueueNameFormat, name);
        }

       
    }
}
