using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFramework.Infrastructure;
using System.Collections;
using Newtonsoft.Json;
using IFramework.Message;
using Microsoft.ServiceBus.Messaging;

namespace IFramework.MessageQueue.MessageFormat
{
    public class MessageContext : IMessageContext
    {
        public BrokeredMessage BrokeredMessage { get; protected set; }

        public MessageContext(BrokeredMessage brokeredMessage)
        {
            BrokeredMessage = brokeredMessage;
            SentTime = DateTime.Now;
        }

        public MessageContext(IMessage message)
        {
            BrokeredMessage = new BrokeredMessage(message.ToJson());
            SentTime = DateTime.Now;
            Message = message;
            MessageID = message.ID;
        }

        public MessageContext(IMessage message, string key)
            : this(message)
        {
            Key = key;
        }

        public MessageContext(IMessage message, string replyToEndPoint, string key)
            : this(message, key)
        {
            ReplyToEndPoint = replyToEndPoint;
        }

        public MessageContext(IMessage message, string replyToEndPoint, string fromEndPoint, string key)
            : this(message, replyToEndPoint, key)
        {
            FromEndPoint = fromEndPoint;
        }

        public IDictionary<string, object> Headers
        {
            get { return BrokeredMessage.Properties; }
        }

        public string Key
        {
            get { return (string)Headers["Key"]; }
            set { Headers["Key"] = value; }
        }

        public string CorrelationID
        {
            get { return BrokeredMessage.CorrelationId; }
            set { BrokeredMessage.CorrelationId = value; }
        }

        public string MessageID
        {
            get { return BrokeredMessage.MessageId; }
            set { BrokeredMessage.MessageId = value; }
        }

        public string ReplyToEndPoint
        {
            get { return BrokeredMessage.ReplyTo; }
            set { BrokeredMessage.ReplyTo = value; }
        }

        public object Reply
        {
            get;
            set;
        }

        public string FromEndPoint
        {
            get { return (string)Headers["FromEndPoint"]; }
            set { Headers["FromEndPoint"] = value; }
        }

        object _Message;
        public object Message
        {
            get
            {
                return _Message ?? (_Message = BrokeredMessage.GetBody<string>()
                                                .ToJsonObject(Type.GetType(Headers["MessageType"].ToString())));
            }
            protected set
            {
                _Message = value;
                Headers["MessageType"] = value.GetType().AssemblyQualifiedName;
            }
        }

        public DateTime SentTime
        {
            get { return (DateTime) Headers["SentTime"]; }
            set { Headers["SentTime"] = value; }
        }
    }
}
