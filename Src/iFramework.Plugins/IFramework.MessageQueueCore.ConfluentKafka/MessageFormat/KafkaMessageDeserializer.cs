using System.Text;
using Confluent.Kafka;
using IFramework.Infrastructure;

namespace IFramework.MessageQueue.ConfluentKafka.MessageFormat
{
    public class KafkaMessageDeserializer<TValue>
    {
        public static Deserializer<TValue> DeserializeValue =>
            (topic, data, isNull) => Encoding.UTF8.GetString(data.ToArray()).ToJsonObject<TValue>();
    }
}