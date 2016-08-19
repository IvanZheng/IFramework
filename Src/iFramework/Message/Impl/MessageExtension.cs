using IFramework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.Message.Impl
{
    public static class MessageExtension
    {
        public static string GetTopic(this IMessage message)
        {
            string topic = null;
            var topicAttribute = message.GetCustomAttribute<TopicAttribute>();
            if (topicAttribute != null && !string.IsNullOrWhiteSpace(topicAttribute.Topic))
            {
                topic = topicAttribute.Topic;
            }
            return topic;
        }
    }
}
