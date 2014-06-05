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
            if (result != null)
            {
                BrokeredMessage = new BrokeredMessage(result.ToJson());
            }
            else
            {
                BrokeredMessage = new BrokeredMessage();
            }
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
                if (_Result != null)
                {
                    return _Result;
                }
                object messageType = null;
                if (Headers.TryGetValue("MessageType", out messageType) && messageType != null)
                {
                    _Result = BrokeredMessage.GetBody<string>()
                                               .ToJsonObject(Type.GetType(messageType.ToString()));
                
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
