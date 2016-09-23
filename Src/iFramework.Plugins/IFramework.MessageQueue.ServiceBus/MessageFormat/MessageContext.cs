using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFramework.Infrastructure;
using System.Collections;
using Newtonsoft.Json;
using IFramework.Message;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json.Linq;
using IFramework.Message.Impl;

namespace IFramework.MessageQueue.ServiceBus.MessageFormat
{
    public class MessageContext : IMessageContext
    {
        public BrokeredMessage BrokeredMessage { get; protected set; }
        public List<IMessageContext> ToBeSentMessageContexts { get; protected set; }
        public Action Complete { get; protected set; }
        public long Offset { get; protected set; }
        public MessageContext(BrokeredMessage brokeredMessage, Action complete = null)
        {
            BrokeredMessage = brokeredMessage;
            SentTime = DateTime.Now;
            Complete = complete;
            Offset = brokeredMessage.SequenceNumber;
            ToBeSentMessageContexts = new List<IMessageContext>();
        }

        public MessageContext(object message, string id = null)
        {
            BrokeredMessage = new BrokeredMessage(message.ToJson());
            SentTime = DateTime.Now;
            Message = message;
            if (!string.IsNullOrEmpty(id))
            {
                MessageID = id;
            }
            else if (message is IMessage)
            {
                MessageID = (message as IMessage).ID;
            }
            else
            {
                MessageID = ObjectId.GenerateNewId().ToString();
            }
            ToBeSentMessageContexts = new List<IMessageContext>();
            if (message != null)
            {
                var topicAttribute = message.GetCustomAttribute<TopicAttribute>();
                if (topicAttribute != null && !string.IsNullOrWhiteSpace(topicAttribute.Topic))
                {
                    Topic = topicAttribute.Topic;
                }
            }

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

        public IDictionary<string, object> Headers
        {
            get { return BrokeredMessage.Properties; }
        }

        SagaInfo _sagaInfo;
        public SagaInfo SagaInfo
        {
            get
            {
                if (_sagaInfo == null)
                {
                    var sagaInfoJson = Headers.TryGetValue("SagaInfo") as JObject;
                    if (sagaInfoJson != null)
                    {
                        try
                        {
                            _sagaInfo = ((JObject)Headers.TryGetValue("SagaInfo")).ToObject<SagaInfo>();
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
                return _sagaInfo;
            }
            set { Headers["SagaInfo"] = _sagaInfo = value; }
        }

        public string Key
        {
            get { return (string)Headers.TryGetValue("Key"); }
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

        object _Message;
        public object Message
        {
            get
            {
                if (_Message != null)
                {
                    return _Message;
                }
                object messageType = null;
                if (Headers.TryGetValue("MessageType", out messageType) && messageType != null)
                {
                    var jsonValue = BrokeredMessage.GetBody<string>();
                    _Message = jsonValue.ToJsonObject(Type.GetType(messageType.ToString()));

                }
                return _Message;
            }
            protected set
            {
                _Message = value;
                if (value != null)
                {
                    Headers["MessageType"] = value.GetType().AssemblyQualifiedName;
                }
            }
        }

        public DateTime SentTime
        {
            get { return (DateTime)Headers.TryGetValue("SentTime"); }
            set { Headers["SentTime"] = value; }
        }

        public string Topic
        {
            get { return (string)Headers.TryGetValue("Topic"); }
            set { Headers["Topic"] = value; }
        }
    }
}
