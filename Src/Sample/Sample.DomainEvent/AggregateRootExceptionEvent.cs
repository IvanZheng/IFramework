using System.Runtime.Serialization;
using IFramework.Event;
using IFramework.Exceptions;
using IFramework.Message;

namespace Sample.DomainEvents
{
    [Topic("DomainEvent")]
    public class AggregateRootExceptionEvent : AggregateRootEvent, IAggregateRootExceptionEvent
    {
        public AggregateRootExceptionEvent() { }
        public AggregateRootExceptionEvent(object aggregateRootId) : base(aggregateRootId) { }
    }
}