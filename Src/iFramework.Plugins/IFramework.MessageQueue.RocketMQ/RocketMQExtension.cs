using System;
using System.Collections.Generic;
using System.Text;

namespace IFramework.MessageQueue.RocketMQ
{
    public static class RocketMQExtension
    {
        public static Org.Apache.Rocketmq.Message.Builder AddProperties(this Org.Apache.Rocketmq.Message.Builder builder, 
                                                                        IDictionary<string, object> properties)
        {
            foreach (var (key, value) in properties)
            {
                builder.AddProperty(key, value?.ToString());
            }

            return builder;
        }
    }
}
