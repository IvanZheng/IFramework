using IFramework.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFramework.Infrastructure;
using System.ComponentModel.DataAnnotations.Schema;

namespace IFramework.EntityFramework.MessageStoring
{
    public abstract class UnSentMessage
    {
        public UnSentMessage() { }
        public UnSentMessage(IMessageContext messageContext)
        {
            ID = messageContext.MessageID;
            CorrelationID = messageContext.CorrelationID;
            MessageBody = messageContext.Message.ToJson();
            CreateTime = messageContext.SentTime;
            Name = messageContext.Message.GetType().Name;
            Type = messageContext.Message.GetType().FullName;
        }

        public string ID { get; set; }
        public string CorrelationID { get; set; }
        public string MessageBody { get; set; }
        public DateTime CreateTime { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
    }
}
