using System.Collections.Generic;

namespace IFramework.MessageQueue.ConfluentKafka.MessageFormat
{
    public class KafkaMessage
    {
        protected KafkaMessage(){}
        public KafkaMessage(object payload = null)
        {
            Headers = new Dictionary<string, object>(System.StringComparer.OrdinalIgnoreCase);
            Payload = payload;
        }

        public IDictionary<string, object> Headers { get; set; }
        public object Payload { get; set; }
    }
}