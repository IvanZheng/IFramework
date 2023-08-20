using System;
using System.Collections.Generic;
using System.Text;
using Confluent.Kafka;
using IFramework.Message;
using IFramework.Message.Impl;

namespace IFramework.MessageQueue.ConfluentKafka.MessageFormat
{
    public class DefaultKafkaMessageContextBuilder:IKafkaMessageContextBuilder<PayloadMessage>
    {
        public IMessageContext Build(ConsumeResult<string, PayloadMessage> message)
        {
            var kafkaMessage = message.Message;
            return new MessageContext(kafkaMessage.Value,
                                      message.Topic,
                                      message.Partition,
                                      message.Offset);
        }
    }
}
