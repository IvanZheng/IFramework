using System.Collections.Generic;

namespace IFramework.MessageQueue.RabbitMQ.MessageFormat
{
    public class RabbitMQMessage
    {
        public RabbitMQMessage(byte[] payload = null)
        {
            Headers = new Dictionary<string, object>();
            Payload = payload;
        }

        public IDictionary<string, object> Headers { get; }
        public byte[] Payload { get; set; }
    }
}