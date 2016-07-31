using IFramework.Config;
using IFramework.Infrastructure;
using System;

namespace IFramework.Config
{
    public static class IFrameworkConfigurationExtension
    {
        static string _MessageQueueNameFormat;
        static TimeSpan _ReceiveMessageTimeout = new TimeSpan(0, 0, 10);
        static string _topicNameFormat;
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

        public static Configuration SetTopicNameFormat(this Configuration configuration, string format)
        {
            _topicNameFormat = format;
            return configuration;
        }

        public static string FormatTopicName(this Configuration configuration, string topic)
        {
            return string.IsNullOrEmpty(_topicNameFormat) ?
                          topic :
                          string.Format(_topicNameFormat, topic);
        }
    }
}
