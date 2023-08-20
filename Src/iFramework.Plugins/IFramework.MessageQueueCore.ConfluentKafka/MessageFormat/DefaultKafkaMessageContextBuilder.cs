using System;
using System.Collections.Generic;
using System.Text;
using Confluent.Kafka;
using IFramework.Infrastructure;
using IFramework.Message;
using IFramework.Message.Impl;

namespace IFramework.MessageQueue.ConfluentKafka.MessageFormat
{
    public class DefaultKafkaMessageContextBuilder:IKafkaMessageContextBuilder
    {
        public IMessageContext Build(ConsumeResult<string, string> message)
        {
            var kafkaMessage = message.Message;
            return new MessageContext(kafkaMessage.Value.ToJsonObject<PayloadMessage>(processDictionaryKeys:false),
                                      message.Topic,
                                      message.Partition,
                                      message.Offset);
        }
    }
}
