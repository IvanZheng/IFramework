using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Confluent.Kafka.Serialization;
using IFramework.Infrastructure;

namespace IFramework.MessageQueue.ConfluentKafka.MessageFormat
{
    public class KafkaMessageDeserializer: IDeserializer<KafkaMessage>
    {
        public KafkaMessage Deserialize(byte[] data)
        {
            return Encoding.UTF8.GetString(data).ToJsonObject<KafkaMessage>();
        }
    }
}
