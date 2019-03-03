using System.Text;
using Confluent.Kafka;
using IFramework.Infrastructure;

namespace IFramework.MessageQueue.ConfluentKafka.MessageFormat
{
    public class KafkaMessageSerializer<TValue>: ISerializer<TValue>
    {
        public byte[] Serialize(TValue data, bool isKey, MessageMetadata messageMetadata, TopicPartition destination)
        {
            var jsonValue = data.ToJson();
            return Encoding.UTF8.GetBytes(jsonValue);
        }
    }
}