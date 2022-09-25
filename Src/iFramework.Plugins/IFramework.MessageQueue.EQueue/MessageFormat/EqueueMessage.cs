using System.Collections.Generic;
using IFramework.Infrastructure;

namespace IFramework.MessageQueue.EQueue.MessageFormat
{
    public class EQueueMessage
    {
        public EQueueMessage(string payload = null)
        {
            Headers = new Dictionary<string, object>(System.StringComparer.OrdinalIgnoreCase);
            Payload = payload.ToJsonObject();
        }

        public IDictionary<string, object> Headers { get; }
        public object Payload { get; set; }
    }
}