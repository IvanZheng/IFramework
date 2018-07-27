using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Confluent.Kafka.Serialization;
using IFramework.Infrastructure;

namespace IFramework.MessageQueue.ConfluentKafka.MessageFormat
{
    public class KafkaMessageDeserializer : IDeserializer<KafkaMessage>
    {
        public IEnumerable<KeyValuePair<string, object>> Configure(IEnumerable<KeyValuePair<string, object>> config, bool isKey)
        {
            return config;
        }

        public KafkaMessage Deserialize(string topic, ReadOnlySpan<byte> data, bool isNull)
        {
            return Encoding.UTF8.GetString(data.ToArray()).ToJsonObject<KafkaMessage>();
        }

        public void Dispose()
        {
            
        }
    }
}
