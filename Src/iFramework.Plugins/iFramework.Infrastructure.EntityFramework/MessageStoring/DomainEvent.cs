using IFramework.Event;
using IFramework.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.EntityFramework
{
    public class DomainEvent : Message
    {
        public string AggregateRootID { get; set; }
        public string AggregateRootType { get; set; }
        public int Version { get; set; }

        public DomainEvent() { }
        public DomainEvent(IMessageContext messageContext, string sourceMessageID) :
            base(messageContext, sourceMessageID)
        {
            var domainEvent = messageContext.Message as IDomainEvent;
            AggregateRootID = domainEvent.AggregateRootID.ToString();
            AggregateRootType = domainEvent.AggregateRootName;
            if (domainEvent is IFramework.Event.DomainEvent)
            {
                Version = (domainEvent as IFramework.Event.DomainEvent).Version;
            }
        }

        public Command Parent
        {
            get
            {
                return ParentMessage as Command;
            }
        }

        public IEnumerable<Command> Children
        {
            get
            {
                return ChildrenMessage.Cast<Command>();
            }
        }
    }
}
