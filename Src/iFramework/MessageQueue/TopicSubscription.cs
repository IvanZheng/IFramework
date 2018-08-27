using System;

namespace IFramework.MessageQueue
{
    public class TopicSubscription
    {
        public TopicSubscription(string topic, Func<string[], bool> tagFilter = null)
        {
            Topic = topic;
            TagFilter = tagFilter;
        }

        public string Topic { get; set; }
        public Func<string[], bool> TagFilter { get; set; }
    }
}