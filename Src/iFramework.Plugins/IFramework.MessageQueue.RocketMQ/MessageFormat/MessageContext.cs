using System;
using System.Collections.Generic;
using IFramework.Infrastructure;
using IFramework.Message;
using IFramework.Message.Impl;

namespace IFramework.MessageQueue.RocketMQ.MessageFormat
{
    public class MessageContext : IMessageContext
    {
        private object _message;

        private SagaInfo _sagaInfo;

        public MessageContext(RocketMQMessage rocketMQMessage, string topic, int partition, long offset)
        {
            RocketMQMessage = rocketMQMessage;
            MessageOffset = new MessageOffset(null, topic, partition, offset);
        }

        public MessageContext(object message, string id = null)
        {
            RocketMQMessage = new RocketMQMessage();
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

        public RocketMQMessage RocketMQMessage { get; protected set; }

        public IDictionary<string, object> Headers => RocketMQMessage.Headers;

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
                            _sagaInfo = sagaInfoJson.ToJson().ToJsonObject<SagaInfo>();
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

        public string Key
        {
            get => Headers.TryGetValue("Key")?.ToString();
            set => Headers["Key"] = value;
        }

        public string[] Tags
        {
            get => Headers.TryGetValue(nameof(Tags))?.ToString().Split(new []{","}, StringSplitOptions.RemoveEmptyEntries);
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

        public string ReplyToEndPoint
        {
            get => Headers.TryGetValue("ReplyToEndPoint")?.ToString();
            set => Headers["ReplyToEndPoint"] = value;
        }

        public object Reply { get; set; }

        public object Message
        {
            get => _message ?? (_message = this.GetMessage(RocketMQMessage.Payload));
            protected set
            {
                _message = value;
                RocketMQMessage.Payload = value;
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
                var timeValue = Headers.TryGetValue("SentTime");
                if (timeValue is DateTime sentTime)
                {
                    return sentTime;
                }
                else
                {
                    if (DateTime.TryParse(timeValue?.ToString(), out var time))
                    {
                        return time;
                    }
                    return time;
                }
            }
            set => Headers["SentTime"] = value;
        }

        public string MessageType
        {
            get => Headers.TryGetValue(nameof(MessageType))?.ToString();
            set => Headers[nameof(MessageType)] = value;
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