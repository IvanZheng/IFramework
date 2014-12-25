using IFramework.Event;
using IFramework.Infrastructure;
using IFramework.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sample.DomainEvents
{
    [Topic("DomainEventTopic")]
    public class DomainEvent : IDomainEvent
    {
        public int Version { get; set; }
        public object AggregateRootID
        {
            get;
            private set;
        }

        public string AggregateRootName
        {
            get;
            set;
        }

        public string ID
        {
            get;
            set;
        }

        public DomainEvent()
        {
            ID = ObjectId.GenerateNewId().ToString();
        }

        public DomainEvent(object aggregateRootID)
            : this()
        {
            AggregateRootID = aggregateRootID;
        }

        
    }
}
