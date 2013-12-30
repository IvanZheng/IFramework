using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.Message
{
    public interface IMessageContext
    {
        Dictionary<string, string> Headers { get;}
        string MessageID { get;}
        string ReplyToEndPoint { get;}
        [Newtonsoft.Json.JsonIgnore]
        object Message { get;}
        DateTime SentTime { get;}
    }
}
