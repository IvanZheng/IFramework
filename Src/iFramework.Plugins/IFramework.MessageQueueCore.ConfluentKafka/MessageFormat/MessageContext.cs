using System;
using System.Collections.Generic;
using System.Text;
using IFramework.Infrastructure;
using IFramework.Message;
using IFramework.Message.Impl;
using IFramework.MessageQueue;

namespace IFramework.MessageQueue.ConfluentKafka.MessageFormat
{
    public class MessageContext : IMessageContext
    {
        private object _message;

        private SagaInfo _sagaInfo;

        public MessageContext(KafkaMessage kafkaMessage, string topic, int partition, long offset)
        {
            KafkaMessage = kafkaMessage;
            MessageOffset = new MessageOffset(null, topic, partition, offset);
        }

        public MessageContext(object message, string id = null)
        {
            KafkaMessage = new KafkaMessage();
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

        public KafkaMessage KafkaMessage { get; protected set; }

        public IDictionary<string, object> Headers => KafkaMessage.Headers;

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
            get => (string) Headers.TryGetValue("Key");
            set => Headers["Key"] = value;
        }

        public string[] Tags
        {
            get => (string[])Headers.TryGetValue(nameof(Tags));
            set => Headers[nameof(Tags)] = value;
        }

        public string CorrelationId
        {
            get => (string) Headers.TryGetValue("CorrelationID");
            set => Headers["CorrelationID"] = value;
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
            get => _message ?? (_message = this.GetMessage(KafkaMessage.Payload));
            protected set
            {
                _message = value;
                KafkaMessage.Payload = value.ToJson();
                if (value != null)
                {
                    Headers["MessageType"] = this.GetMessageCode(value.GetType());;
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