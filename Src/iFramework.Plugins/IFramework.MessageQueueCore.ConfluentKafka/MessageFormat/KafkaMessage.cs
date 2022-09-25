using IFramework.Infrastructure;
using System.Collections.Generic;

namespace IFramework.MessageQueue.ConfluentKafka.MessageFormat
{
    public class KafkaMessage
    {
        protected KafkaMessage(){}
        public KafkaMessage(string payloadJson = null)
        {
            Headers = new Dictionary<string, object>(System.StringComparer.OrdinalIgnoreCase);
            Payload = payloadJson.ToJsonObject();
        }

        public IDictionary<string, object> Headers { get; set; }
        public object Payload { get; set; }
    }
}