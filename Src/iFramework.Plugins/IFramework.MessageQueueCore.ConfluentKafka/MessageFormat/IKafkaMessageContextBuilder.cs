using System;
using System.Collections.Generic;
using System.Text;
using Confluent.Kafka;
using IFramework.Message;
using IFramework.MessageQueue.Client.Abstracts;

namespace IFramework.MessageQueue.ConfluentKafka.MessageFormat
{
    public interface IKafkaMessageContextBuilder<TKafkaMessage>: IMessageContextBuilder<TKafkaMessage>
    {
        IMessageContext Build(ConsumeResult<string, TKafkaMessage> consumeResult);
    }
}
