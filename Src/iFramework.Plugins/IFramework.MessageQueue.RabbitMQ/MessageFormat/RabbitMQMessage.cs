using System.Collections.Generic;

namespace IFramework.MessageQueue.RabbitMQ.MessageFormat
{
    public class RabbitMQMessage
    {
        protected RabbitMQMessage(){}
        public RabbitMQMessage(object payload = null)
        {
            Headers = new Dictionary<string, object>(System.StringComparer.OrdinalIgnoreCase);
            Payload = payload;
        }

        public IDictionary<string, object> Headers { get; set; }
        public object Payload { get; set; }
    }
}