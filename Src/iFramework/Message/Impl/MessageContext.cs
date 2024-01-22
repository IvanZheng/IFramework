using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using IFramework.Infrastructure;
using IFramework.MessageQueue;

namespace IFramework.Message.Impl
{
    public class MessageContext : IMessageContext
    {
        private object _message;

        private SagaInfo _sagaInfo;
        private DateTime _sentTime;
        public MessageContext(PayloadMessage payloadMessage, MessageOffset messageOffset)
        {
            PayloadMessage = payloadMessage;
            MessageOffset = messageOffset;
        }

        public MessageContext(PayloadMessage payloadMessage, string topic, int partition, long offset)
        {
            PayloadMessage = payloadMessage;
            MessageOffset = new MessageOffset(null, topic, partition, offset);
        }

        public MessageContext(object message, string topic, int partition, long offset, object queueMessage = null, IDictionary<string, string> headers = null)
        {
            PayloadMessage = new PayloadMessage
            {
                Headers = headers
            };

            Message = message;
            MessageOffset = new MessageOffset(null, topic, partition, offset, queueMessage);
        }

        public MessageContext(object message, string id = null)
        {
            PayloadMessage = new PayloadMessage();
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

        public PayloadMessage PayloadMessage { get; protected set; }

        public IDictionary<string, string> Headers => PayloadMessage.Headers;

        public SagaInfo SagaInfo
        {
            get
            {
                if (_sagaInfo == null)
                {
                    var sagaInfoJson = Headers.TryGetValue("SagaInfo");
                    if (sagaInfoJson != null)
                    {
                        try
                        {
                            _sagaInfo = sagaInfoJson.ToJsonObject<SagaInfo>();
                        }
                        catch (Exception)
                        {
                            // ignored
                        }
                    }
                }
                return _sagaInfo;
            }
            set => Headers["SagaInfo"] = (_sagaInfo = value).ToJson();
        }

        public string Key
        {
            get => Headers.TryGetValue("Key");
            set => Headers["Key"] = value;
        }

        public string[] Tags
        {
            get => Headers.TryGetValue(nameof(Tags))?.Split(new []{","}, StringSplitOptions.RemoveEmptyEntries);
            set => Headers[nameof(Tags)] = value?.Length > 0 ? string.Join(",", value):null;
        }

        public string CorrelationId
        {
            get => Headers.TryGetValue("CorrelationId")?.ToString();
            set => Headers["CorrelationId"] = value;
        }

        public string MessageId
        {
            get => Headers.TryGetValue("MessageId")?.ToString();
            set => Headers["MessageId"] = value;
        }

        public string MessageType
        {
            get => Headers.TryGetValue(nameof(MessageType))?.ToString();
            set => Headers[nameof(MessageType)] = value;
        }

        public string ReplyToEndPoint
        {
            get => Headers.TryGetValue("ReplyToEndPoint")?.ToString();
            set => Headers["ReplyToEndPoint"] = value;
        }

        public object Reply { get; set; }

        public object Message
        {
            get => _message ?? (_message = this.GetMessage(PayloadMessage.Payload));
            protected set
            {
                _message = value;
                PayloadMessage.Payload = value;
                if (value != null)
                {
                    Headers["MessageType"] = this.GetMessageCode(value.GetType());;
                }
            }
        }

        public DateTime SentTime
        {
            get
            {
                if (_sentTime != DateTime.MinValue)
                {
                    return _sentTime;
                }
                var timeValue = Headers.TryGetValue("SentTime");
                DateTime.TryParse(timeValue, out _sentTime);
                return _sentTime;
            }
            set => Headers["SentTime"] = value.ToString(CultureInfo.InvariantCulture);
        }

        public string Topic
        {
            get => Headers.TryGetValue("Topic")?.ToString();
            set => Headers["Topic"] = value;
        }

        public string Ip
        {
            get => Headers.TryGetValue("IP")?.ToString();
            set => Headers["IP"] = value;
        }

        public string Producer
        {
            get => Headers.TryGetValue("Producer")?.ToString();
            set => Headers["Producer"] = value;
        }

        public MessageOffset MessageOffset { get; }
    }
}