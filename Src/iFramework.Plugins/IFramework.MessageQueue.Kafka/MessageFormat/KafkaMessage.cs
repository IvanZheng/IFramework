using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.MessageQueue.MSKafka.MessageFormat
{
    public class KafkaMessage
    {
        public IDictionary<string, object> Headers { get; private set; }
        public byte[] Payload { get; set; }

        public KafkaMessage(byte[] payload = null)
        {
            Headers = new Dictionary<string, object>();
            Payload = payload;
        }
    }
}
