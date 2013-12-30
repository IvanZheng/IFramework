using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.Event
{
    public interface IDomainEvent
    {
        object AggregateRootID { get;}
    }
}
