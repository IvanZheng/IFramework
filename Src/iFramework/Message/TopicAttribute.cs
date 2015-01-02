using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.Message
{
    public class TopicAttribute : Attribute
    {
        public string Topic { get; set; }

        public TopicAttribute(string topic)
        {
            Topic = topic;
        }
    }
}
