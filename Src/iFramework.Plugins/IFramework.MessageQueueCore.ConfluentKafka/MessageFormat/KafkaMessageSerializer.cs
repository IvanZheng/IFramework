using System.Text;
using Confluent.Kafka;
using IFramework.Infrastructure;

namespace IFramework.MessageQueue.ConfluentKafka.MessageFormat
{
    public class KafkaMessageSerializer<TValue>
    {
        public static Serializer<TValue> SerializeValue = (topic, value) =>
        {
            var jsonValue = value.ToJson();
            return Encoding.UTF8.GetBytes(jsonValue);
        };
    }
}