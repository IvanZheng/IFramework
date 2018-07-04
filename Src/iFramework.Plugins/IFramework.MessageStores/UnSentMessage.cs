using System;
using IFramework.Infrastructure;
using IFramework.Message;
using IFramework.Message.Impl;

namespace IFramework.MessageStores.Relational
{
    public abstract class UnSentMessage
    {
        protected UnSentMessage() { }

        protected UnSentMessage(IMessageContext messageContext)
        {
            Id = messageContext.MessageId;
            CorrelationId = messageContext.CorrelationId;
            MessageBody = messageContext.Message.ToJson();
            ReplyToEndPoint = messageContext.ReplyToEndPoint;
            SagaInfo = messageContext.SagaInfo?.Clone() ?? SagaInfo.Null;
            CreateTime = messageContext.SentTime;
            if (messageContext.Message != null)
            {
                Name = messageContext.Message.GetType().Name;
                Type = messageContext.Message.GetType().AssemblyQualifiedName;
            }
            Topic = messageContext.Topic;
        }

        public string Id { get; set; }
        public string ReplyToEndPoint { get; set; }
        public SagaInfo SagaInfo { get; set; }
        public string CorrelationId { get; set; }
        public string MessageBody { get; set; }
        public DateTime CreateTime { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Topic { get; set; }
        public string Ip { get; set; }
        public string Producer { get; set; }
    }
}