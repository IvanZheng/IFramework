using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.Event
{
    public interface IDomainEvent : IEvent
    {
        object AggregateRootID { get;}
        string AggregateRootName { get; set; }
        int Version { get; set; }
    }
}
