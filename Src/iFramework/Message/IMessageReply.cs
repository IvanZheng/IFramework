using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.Message
{
    public interface IMessageReply
    {
        IDictionary<string, object> Headers { get; }
        string MessageID { get; }
        object Result { get; }
    }
}
