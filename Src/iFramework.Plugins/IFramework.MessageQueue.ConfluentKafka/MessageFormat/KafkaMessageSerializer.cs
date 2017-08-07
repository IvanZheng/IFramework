using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Confluent.Kafka.Serialization;
using IFramework.Infrastructure;

namespace IFramework.MessageQueue.ConfluentKafka.MessageFormat
{
    public class KafkaMessageSerializer: ISerializer<KafkaMessage>
    {
        public byte[] Serialize(KafkaMessage data)
        {
            var jsonValue = data.ToJson();
            return Encoding.UTF8.GetBytes(jsonValue);
        }
    }
}
