using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFramework.Infrastructure;
using IFramework.Message.Impl;
using Newtonsoft.Json;
using IFramework.Message;

namespace IFramework.MessageQueue.ZeroMQ.MessageFormat
{
    public class MessageReply : IMessageReply
    {
        public MessageReply()
        {
            Headers = new Dictionary<string, object>();
        }

        public MessageReply(string messageID, object result)
            : this()
        {
            MessageID = messageID;
            Result = result;
        }

        public IDictionary<string, object> Headers
        {
            get;
            set;
        }

        public string MessageID
        {
            get;
            set;
        }

        object _Result;
        public object Result
        {
            get
            {
                if (_Result != null)
                {
                    return _Result;
                }
                object messageType = null;
                object messageBody = null;
                if (Headers.TryGetValue("MessageType", out messageType) && messageType != null
                   && Headers.TryGetValue("Message", out messageBody) && messageBody != null)
                {
                    _Result = messageBody.ToString().ToJsonObject(Type.GetType(messageType.ToString()));
                
                }
                return _Result;
            }
            set
            {
                _Result = value;
                if (_Result != null)
                {
                    _Result = value;
                    Headers["Message"] = _Result.ToJson();
                    Headers["MessageType"] = _Result.GetType().AssemblyQualifiedName;
                }
            }
        }
    }
}
