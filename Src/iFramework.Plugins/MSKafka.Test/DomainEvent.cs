using IFramework.Event;
using IFramework.Infrastructure;
using IFramework.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSKafka.Test
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

        public string ID
        {
            get;
            set;
        }

        public string Body { get; set; }

        public DomainEvent()
        {

        }

        public DomainEvent(string body)
        {
            Body = body;
            ID = ObjectId.GenerateNewId().ToString();
        }



        public DomainEvent(object aggregateRootID, string body)
            : this(body)
        {
            AggregateRootID = aggregateRootID;
        }

        
    }
}
