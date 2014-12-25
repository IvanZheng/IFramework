using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFramework.Infrastructure;
using System.Collections;
using Newtonsoft.Json;
using IFramework.Message;
using EQueueProtocols = EQueue.Protocols;

namespace IFramework.MessageQueue.EQueue.MessageFormat
{
    public class MessageContext : IMessageContext
    {
        public string Topic { get; set; }

        EQueueProtocols.Message _EQueueMessage;
        [JsonIgnore]
        public EQueueProtocols.Message EQueueMessage
        {
            get
            {
                if (_EQueueMessage == null)
                {
                    _EQueueMessage = new EQueueProtocols.Message(Topic, Encoding.UTF8.GetBytes(this.ToJson()));
                }
                return _EQueueMessage;
            }
            protected set
            {
                _EQueueMessage = value;
            }
        }

        public MessageContext()
        {
            Headers = new Dictionary<string, object>();
            CorrelationID = null;
            Key = null;
            MessageID = null;
            CorrelationID = null;
            ReplyToEndPoint = null;
            Reply = null;
            FromEndPoint = null;
        }

        public MessageContext(IMessage message)
            : this()
        {
            SentTime = DateTime.Now;
            Message = message;
            MessageID = message.ID;
            ToBeSentMessageContexts = new List<IMessageContext>();
        }

        public MessageContext(string topic, IMessage message)
            : this(message)
        {
            var topicAttribute = message.GetCustomAttribute<TopicAttribute>();
            if (topicAttribute != null && !string.IsNullOrWhiteSpace(topicAttribute.Topic))
            {
                Topic = topicAttribute.Topic;
            }
            else
            {
                Topic = topic;
            }
        }

        public MessageContext(string topic, IMessage message, string key)
            : this(topic, message)
        {
            Key = key;
        }

        public MessageContext(string topic, IMessage message, string replyToEndPoint, string key)
            : this(topic, message, key)
        {
            ReplyToEndPoint = replyToEndPoint;
        }

        public MessageContext(string topic, IMessage message, string replyToEndPoint, string fromEndPoint, string key)
            : this(topic, message, replyToEndPoint, key)
        {
            FromEndPoint = fromEndPoint;
        }

        public IDictionary<string, object> Headers
        {
            get;
            set;
        }

        public string Key
        {
            get { return (string)Headers["Key"]; }
            set { Headers["Key"] = value; }
        }

        public string CorrelationID
        {
            get { return (string)Headers["CorrelationID"]; }
            set { Headers["CorrelationID"] = value; }
        }

        public string MessageID
        {
            get { return (string)Headers["MessageID"]; }
            set { Headers["MessageID"] = value; }
        }

        public string ReplyToEndPoint
        {
            get { return (string)Headers["ReplyToEndPoint"]; }
            set { Headers["ReplyToEndPoint"] = value; }
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
        [JsonIgnore]
        public object Message
        {
            get
            {
                if (_Message != null)
                {
                    return _Message;
                }
                object messageType = null;
                object messageBody = null;
                if (Headers.TryGetValue("MessageType", out messageType) && messageType != null
                   && Headers.TryGetValue("Message", out messageBody) && messageBody != null)
                {
                    _Message = messageBody.ToString().ToJsonObject(Type.GetType(messageType.ToString()));

                }
                return _Message;
            }
            set
            {
                _Message = value;
                Headers["Message"] = _Message.ToJson();
                Headers["MessageType"] = _Message.GetType().AssemblyQualifiedName;
            }
        }


        public DateTime SentTime
        {
            get { return (DateTime)Headers["SentTime"]; }
            set { Headers["SentTime"] = value; }
        }

        [JsonIgnore]
        public List<IMessageContext> ToBeSentMessageContexts { get; set; }
    }
}
