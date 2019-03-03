using System;
using System.Text;
using Confluent.Kafka;
using IFramework.Infrastructure;

namespace IFramework.MessageQueue.ConfluentKafka.MessageFormat
{
    public class KafkaMessageDeserializer<TValue>: IDeserializer<TValue>
    {
        public TValue Deserialize(ReadOnlySpan<byte> data, bool isNull, bool isKey, MessageMetadata messageMetadata, TopicPartition source)
        {
            return  Encoding.UTF8.GetString(data.ToArray()).ToJsonObject<TValue>();
        }
    }
}