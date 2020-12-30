using System;
using System.Collections.Generic;
using System.Text;
using IFramework.Infrastructure;
using IFramework.Message;
using IFramework.Message.Impl;
using Newtonsoft.Json.Linq;

namespace IFramework.MessageQueue.RabbitMQ.MessageFormat
{
    public class MessageContext : IMessageContext
    {
        private object _message;

        private SagaInfo _sagaInfo;

        public MessageContext(RabbitMQMessage rabbitMQMessage, MessageOffset deliveryTag)
        {
            RabbitMQMessage = rabbitMQMessage;
            ToBeSentMessageContexts = new List<IMessageContext>();
            MessageOffset = deliveryTag;
        }

        public MessageContext(object message, string id = null)
        {
            RabbitMQMessage = new RabbitMQMessage();
            SentTime = DateTime.Now;
            Message = message;
            if (!string.IsNullOrEmpty(id))
            {
                MessageId = id;
            }
            else if (message is IMessage)
            {
                MessageId = ((IMessage) message).Id;
            }
            else
            {
                MessageId = ObjectId.GenerateNewId().ToString();
            }
            ToBeSentMessageContexts = new List<IMessageContext>();
            if (message is IMessage iMessage)
            {
                Topic = iMessage.GetTopic();
                Tags = iMessage.Tags;
            }
            MessageOffset = new MessageOffset();
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

        public RabbitMQMessage RabbitMQMessage { get; protected set; }
  
        public List<IMessageContext> ToBeSentMessageContexts { get; protected set; }
      

        public IDictionary<string, object> Headers => RabbitMQMessage.Headers;

        public string Key
        {
            get => (string) Headers.TryGetValue("Key");
            set => Headers["Key"] = value;
        }
        public string[] Tags
        {
            get => Headers.TryGetValue(nameof(Tags))?.ToString().Split(new []{","}, StringSplitOptions.RemoveEmptyEntries);
            set => Headers[nameof(Tags)] = value?.Length > 0 ? string.Join(",", value):null;
        }

        public string CorrelationId
        {
            get => (string) Headers.TryGetValue("CorrelationId");
            set => Headers["CorrelationId"] = value;
        }

        public string MessageId
        {
            get => (string) Headers.TryGetValue("MessageId");
            set => Headers["MessageId"] = value;
        }

        public string ReplyToEndPoint
        {
            get => (string) Headers.TryGetValue("ReplyToEndPoint");
            set => Headers["ReplyToEndPoint"] = value;
        }

        public object Reply { get; set; }

        public object Message
        {
            get => _message ?? (_message = this.GetMessage(RabbitMQMessage.Payload));
            protected set
            {
                _message = value;
                RabbitMQMessage.Payload = value;
                if (value != null)
                {
                    Headers["MessageType"] = this.GetMessageCode(value.GetType());
                }
            }
        }

        public DateTime SentTime
        {
            get => (DateTime) Headers.TryGetValue("SentTime");
            set => Headers["SentTime"] = value;
        }

        public string Topic
        {
            get => (string) Headers.TryGetValue("Topic");
            set => Headers["Topic"] = value;
        }

        public SagaInfo SagaInfo
        {
            get
            {
                if (_sagaInfo == null)
                {
                    if (Headers.TryGetValue("SagaInfo") is JObject sagaInfoJson)
                    {
                        try
                        {
                            _sagaInfo = sagaInfoJson.ToObject<SagaInfo>();
                        }
                        catch (Exception)
                        {
                            // ignored
                        }
                    }
                }
                return _sagaInfo;
            }
            set => Headers["SagaInfo"] = _sagaInfo = value;
        }

        public string Ip
        {
            get => (string) Headers.TryGetValue("IP");
            set => Headers["IP"] = value;
        }

        public string Producer
        {
            get => (string) Headers.TryGetValue("Producer");
            set => Headers["Producer"] = value;
        }

        public MessageOffset MessageOffset { get; }
    }
}