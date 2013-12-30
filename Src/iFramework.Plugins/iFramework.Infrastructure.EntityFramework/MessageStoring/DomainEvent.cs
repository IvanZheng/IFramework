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

        public DomainEvent() { }
        public DomainEvent(IMessageContext messageContext, string sourceMessageID) :
            base(messageContext, sourceMessageID)
        {
            AggregateRootID = (messageContext.Message as IDomainEvent).AggregateRootID.ToString();
            string aggregateRootType = null;
            if (messageContext.Headers.TryGetValue("ARType", out aggregateRootType))
            {
                AggregateRootType = aggregateRootType;
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
