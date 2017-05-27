using System;

namespace IFramework.Message
{
    public class TopicAttribute : Attribute
    {
        public TopicAttribute(string topic)
        {
            Topic = topic;
        }

        public string Topic { get; set; }
    }
}