using IFramework.Event;
using IFramework.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.EntityFramework.MessageStoring
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
                if (domainEvent is IFramework.Event.DomainEvent)
                {
                    Version = (domainEvent as IFramework.Event.DomainEvent).Version;
                }
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
