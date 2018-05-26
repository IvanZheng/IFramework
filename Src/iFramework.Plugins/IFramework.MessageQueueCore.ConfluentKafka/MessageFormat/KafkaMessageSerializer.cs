using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Confluent.Kafka.Serialization;
using IFramework.Infrastructure;

namespace IFramework.MessageQueueCore.ConfluentKafka.MessageFormat
{
    public class KafkaMessageSerializer: ISerializer<KafkaMessage>
    {
        public byte[] Serialize(string topic, KafkaMessage data)
        {
            var jsonValue = data.ToJson();
            return Encoding.UTF8.GetBytes(jsonValue);
        }

        public IEnumerable<KeyValuePair<string, object>> Configure(IEnumerable<KeyValuePair<string, object>> config, bool isKey)
        {
            return config;
        }

        public void Dispose() { }
    }
}
