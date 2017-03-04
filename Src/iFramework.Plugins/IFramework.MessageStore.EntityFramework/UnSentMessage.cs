using IFramework.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFramework.Infrastructure;
using System.ComponentModel.DataAnnotations.Schema;
using IFramework.Message.Impl;

namespace IFramework.MessageStoring
{
    public abstract class UnSentMessage
    {
        public UnSentMessage() { }
        public UnSentMessage(IMessageContext messageContext)
        {
            ID = messageContext.MessageID;
            CorrelationID = messageContext.CorrelationID;
            MessageBody = messageContext.Message.ToJson();
            ReplyToEndPoint = messageContext.ReplyToEndPoint;
            SagaInfo = messageContext.SagaInfo ?? new SagaInfo();
            CreateTime = messageContext.SentTime;
            if (messageContext.Message != null)
            {
                Name = messageContext.Message.GetType().Name;
                Type = messageContext.Message.GetType().AssemblyQualifiedName;
            }
            Topic = messageContext.Topic;
        }
        
        public string ID { get; set; }
        public string ReplyToEndPoint { get; set; }
        public SagaInfo SagaInfo { get; set; }
        public string CorrelationID { get; set; }
        public string MessageBody { get; set; }
        public DateTime CreateTime { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Topic { get; set; }
        public string IP { get; set; }
        public string Producer { get; set; }
    }
}
