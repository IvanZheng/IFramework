using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.Message
{
    public interface IMessageContext
    {
        IDictionary<string, object> Headers { get; }
        string Key { get; }
        string MessageID { get; }
        string CorrelationID { get; set; }
        string ReplyToEndPoint { get; }
        object Reply { get; set; }
        string FromEndPoint { get; set; }
        object Message { get; }
        DateTime SentTime { get; }
        string Topic { get; }
    }
}
