using System;
using IFramework.Event;
using IFramework.Message;

namespace IFramework.MessageStores.Relational
{
    [Obsolete]
    public class Event : Message
    {
        public Event() { }

        public Event(IMessageContext messageContext) :
            base(messageContext)
        {
            if (messageContext.Message is IAggregateRootEvent domainEvent)
            {
                AggregateRootId = domainEvent.AggregateRootId.ToString();
                AggregateRootType = domainEvent.AggregateRootName;
                Version = domainEvent.Version;
            }
        }

        public string AggregateRootId { get; set; }
        public string AggregateRootType { get; set; }

        public int Version { get; set; }
    }
}