using IFramework.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFramework.Infrastructure;
using System.ComponentModel.DataAnnotations.Schema;

namespace IFramework.MessageStoring
{
    public abstract class Message
    {
        public Message() { }
        public Message(IMessageContext messageContext)
        {
            ID = messageContext.MessageID;
            Topic = messageContext.Topic;
            CorrelationID = messageContext.CorrelationID;
            MessageBody = messageContext.Message.ToJson();
            CreateTime = messageContext.SentTime;
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
        //[ForeignKey("CorrelationID")]
        //public virtual Message ParentMessage { get; set; }
        //[InverseProperty("ParentMessage")]
        //public virtual ICollection<Message> ChildrenMessage { get; set; }
    }
}
