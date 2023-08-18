using System;
using System.Collections.Generic;
using System.Text;

namespace IFramework.MessageQueue.RocketMQ.MessageFormat
{
    public class RocketMQMessage
    {
        protected RocketMQMessage(){}
        public RocketMQMessage(object payload = null)
        {
            Headers = new Dictionary<string, object>(System.StringComparer.OrdinalIgnoreCase);
            Payload = payload;
        }

        public IDictionary<string, object> Headers { get; set; }
        public object Payload { get; set; }
    }
}
