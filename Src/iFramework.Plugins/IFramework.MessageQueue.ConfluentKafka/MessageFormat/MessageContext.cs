using System;
using System.Collections.Generic;
using System.Text;
using IFramework.Infrastructure;
using IFramework.Message;
using IFramework.Message.Impl;
using Newtonsoft.Json.Linq;

namespace IFramework.MessageQueue.ConfluentKafka.MessageFormat
{
    public class MessageContext : IMessageContext
    {
        private object _Message;

        private SagaInfo _sagaInfo;

        public MessageContext(KafkaMessage kafkaMessage, int partition, long offset)
        {
            KafkaMessage = kafkaMessage;
            Offset = offset;
            Partition = partition;
            ToBeSentMessageContexts = new List<IMessageContext>();
        }

        public MessageContext(object message, string id = null)
        {
            KafkaMessage = new KafkaMessage();
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
            if (message != null && message is IMessage)
            {
                Topic = (message as IMessage).GetTopic();
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

        public KafkaMessage KafkaMessage { get; protected set; }
        public int Partition { get; protected set; }
        public List<IMessageContext> ToBeSentMessageContexts { get; protected set; }
        public long Offset { get; protected set; }

        public IDictionary<string, object> Headers => KafkaMessage.Headers;

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
                            _sagaInfo = ((JObject) Headers.TryGetValue("SagaInfo")).ToObject<SagaInfo>();
                        }
                        catch (Exception) { }
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

        public string CorrelationID
        {
            get => (string) Headers.TryGetValue("CorrelationID");
            set => Headers["CorrelationID"] = value;
        }

        public string MessageID
        {
            get => (string) Headers.TryGetValue("MessageID");
            set => Headers["MessageID"] = value;
        }

        public string ReplyToEndPoint
        {
            get => (string) Headers.TryGetValue("ReplyToEndPoint");
            set => Headers["ReplyToEndPoint"] = value;
        }

        public object Reply { get; set; }

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
                    var jsonValue = Encoding.UTF8.GetString(KafkaMessage.Payload);
                    _Message = jsonValue.ToJsonObject(Type.GetType(messageType.ToString()));
                }
                return _Message;
            }
            protected set
            {
                _Message = value;
                KafkaMessage.Payload = Encoding.UTF8.GetBytes(value.ToJson());
                if (value != null)
                {
                    Headers["MessageType"] = value.GetType().AssemblyQualifiedName;
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

        public string IP
        {
            get => (string) Headers.TryGetValue("IP");
            set => Headers["IP"] = value;
        }

        public string Producer
        {
            get => (string) Headers.TryGetValue("Producer");
            set => Headers["Producer"] = value;
        }
    }
}