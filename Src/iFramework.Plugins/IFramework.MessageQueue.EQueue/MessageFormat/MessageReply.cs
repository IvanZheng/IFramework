using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFramework.Infrastructure;
using IFramework.Message.Impl;
using Newtonsoft.Json;
using IFramework.Message;
using EQueueProtocols = EQueue.Protocols;

namespace IFramework.MessageQueue.EQueue.MessageFormat
{
    public class MessageReply : IMessageReply
    {
        public EQueueProtocols.Message Message { get; protected set; }

        public MessageReply(EQueueProtocols.Message message)
        {
            Message = message;
        }

        public MessageReply(string topic, string messageID, object result)
        {
            if (result != null)
            {
                Message = new EQueueProtocols.Message(topic, Encoding.UTF8.GetBytes(result.ToJson()));
            }
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
                if (Headers.TryGetValue("MessageType", out messageType) && messageType != null)
                {
                    _Result = Message.Body.GetMessage(Type.GetType(messageType.ToString()));
                
                }
                return _Result;
            }
            set
            {
                _Result = value;
                if (_Result != null)
                {
                    Headers["MessageType"] = _Result.GetType().AssemblyQualifiedName;
                }
            }
        }
    }
}
