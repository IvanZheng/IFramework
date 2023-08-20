using System.Collections.Generic;

namespace IFramework.Message.Impl
{
    public class PayloadMessage
    {
        public PayloadMessage(object payload = null)
        {
            Headers = new Dictionary<string, object>(System.StringComparer.OrdinalIgnoreCase);
            Payload = payload;
        }

        public IDictionary<string, object> Headers { get; }
        public object Payload { get; set; }
    }
}