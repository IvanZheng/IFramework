using System.Collections.Generic;

namespace IFramework.Message.Impl
{
    public class PayloadMessage
    {
        public PayloadMessage(object payload = null)
        {
            Headers = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
            Payload = payload;
        }

        public IDictionary<string, string> Headers { get; set; }
        public object Payload { get; set; }
    }
}