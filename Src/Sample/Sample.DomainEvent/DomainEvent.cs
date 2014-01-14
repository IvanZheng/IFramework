using IFramework.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sample.DomainEvent
{
    public class DomainEvent : IDomainEvent
    {
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
