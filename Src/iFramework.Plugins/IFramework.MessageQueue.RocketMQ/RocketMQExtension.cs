using System;
using System.Collections.Generic;
using System.Text;
using IFramework.Infrastructure;
using IFramework.Message.Impl;

namespace IFramework.MessageQueue.RocketMQ
{
    public static class RocketMQExtension
    {
        public static Org.Apache.Rocketmq.Message.Builder AddProperties(this Org.Apache.Rocketmq.Message.Builder builder, 
                                                                        IDictionary<string, string> properties)
        {
            foreach (var (key, value) in properties)
            {
                if (value != null)
                {
                    builder.AddProperty(key, value);
                }
            }

            return builder;
        }
    }
}
