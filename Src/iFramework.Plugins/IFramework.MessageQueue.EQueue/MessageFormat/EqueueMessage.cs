using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.MessageQueue.EQueue.MessageFormat
{
    public class EQueueMessage
    {
        public IDictionary<string, object> Headers { get; private set; }
        public byte[] Payload { get; set; }

        public EQueueMessage(byte[] payload = null)
        {
            Headers = new Dictionary<string, object>();
            Payload = payload;
        }
    }
}
