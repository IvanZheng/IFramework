using System;
using IFramework.Infrastructure;
using IFramework.Message;
using IFramework.Message.Impl;

namespace IFramework.MessageStoring
{
    public abstract class Message
    {
        public Message()
        {
        }

        public Message(IMessageContext messageContext)
        {
            ID = messageContext.MessageID;
            Topic = messageContext.Topic;
            CorrelationID = messageContext.CorrelationID;
            MessageBody = messageContext.Message.ToJson();
            CreateTime = messageContext.SentTime;
            SagaInfo = messageContext.SagaInfo ?? new SagaInfo();
            IP = messageContext.IP;
            Producer = messageContext.Producer;
            if (messageContext.Message != null)
            {
                Name = messageContext.Message.GetType().Name;
                Type = messageContext.Message.GetType().AssemblyQualifiedName;
            }
        }

        public string ID { get; set; }
        public string CorrelationID { get; set; }
        public string MessageBody { get; set; }
        public DateTime CreateTime { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Topic { get; set; }
        public SagaInfo SagaInfo { get; set; }
        public string IP { get; set; }
        public string Producer { get; set; }
    }
}