using IFramework.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.Event
{
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

        public DomainEvent(object aggregateRootID)
        {
            AggregateRootID = aggregateRootID;
        }
    }
}
