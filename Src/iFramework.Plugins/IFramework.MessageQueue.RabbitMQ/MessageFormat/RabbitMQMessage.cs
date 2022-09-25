using IFramework.Infrastructure;
using System.Collections.Generic;

namespace IFramework.MessageQueue.RabbitMQ.MessageFormat
{
    public class RabbitMQMessage
    {
        protected RabbitMQMessage(){}
        public RabbitMQMessage(string payload = null)
        {
            Headers = new Dictionary<string, object>(System.StringComparer.OrdinalIgnoreCase);
            Payload = payload.ToJsonObject();
        }

        public IDictionary<string, object> Headers { get; set; }
        public object Payload { get; set; }
    }
}