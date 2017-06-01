using IFramework.Config;
using IFramework.Infrastructure;

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
            if (string.IsNullOrEmpty(topic))
            {
                topic = Configuration.Instance.GetDefaultTopic();
            }
            return topic;
        }

        public static string GetFormatTopic(this IMessage message)
        {
            var topic = message.GetTopic();
            if (!string.IsNullOrEmpty(topic))
            {
                topic = Configuration.Instance.FormatAppName(topic);
            }
            return topic;
        }
    }
}