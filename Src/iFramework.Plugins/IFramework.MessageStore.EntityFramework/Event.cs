using IFramework.Event;
using IFramework.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.MessageStoring
{
    public class Event : Message
    {
        public string AggregateRootID { get; set; }
        public string AggregateRootType { get; set; }
        public int Version { get; set; }

        public Event() { }
        public Event(IMessageContext messageContext) :
            base(messageContext)
        {
            var domainEvent = messageContext.Message as IDomainEvent;
            if (domainEvent != null)
            {
                AggregateRootID = domainEvent.AggregateRootID.ToString();
                AggregateRootType = domainEvent.AggregateRootName;
                Version = domainEvent.Version;
            }
        }

        //public Message Parent
        //{
        //    get
        //    {
        //        return ParentMessage;
        //    }
        //}

        //public IEnumerable<Message> Children
        //{
        //    get
        //    {
        //        return ChildrenMessage;
        //    }
        //}
    }
}
