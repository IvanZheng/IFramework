using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFramework.Infrastructure;
using IFramework.Message.Impl;
using Newtonsoft.Json;
using IFramework.Message;
using Microsoft.ServiceBus.Messaging;

namespace IFramework.MessageQueue.MessageFormat
{
    public class MessageReply : IMessageReply
    {
        public BrokeredMessage BrokeredMessage { get; protected set; }

        public MessageReply(BrokeredMessage brokeredMessage)
        {
            BrokeredMessage = brokeredMessage;
        }

        public MessageReply(string messageID, object result)
        {
            BrokeredMessage = new BrokeredMessage(result.ToJson());
            MessageID = messageID;
            Result = result;
        }

        public IDictionary<string, object> Headers
        {
            get { return BrokeredMessage.Properties; }
        }

        public string MessageID
        {
            get { return BrokeredMessage.MessageId; }
            set { BrokeredMessage.MessageId = value; }
        }

        object _Result;
        public object Result
        {
            get
            {
                return _Result ?? (_Result = BrokeredMessage.GetBody<string>()
                                               .ToJsonObject(Type.GetType(Headers["MessageType"].ToString())));
                
            }
            set
            {
                _Result = value;
                Headers["MessageType"] = _Result.GetType().AssemblyQualifiedName;
            }
        }
    }
}
