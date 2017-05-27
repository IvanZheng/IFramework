using System.Collections.Generic;

namespace IFramework.MessageQueue.MSKafka.MessageFormat
{
    public class KafkaMessage
    {
        public KafkaMessage(byte[] payload = null)
        {
            Headers = new Dictionary<string, object>();
            Payload = payload;
        }

        public IDictionary<string, object> Headers { get; }
        public byte[] Payload { get; set; }
    }
}