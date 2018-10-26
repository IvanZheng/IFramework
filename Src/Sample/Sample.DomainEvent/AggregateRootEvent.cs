using IFramework.Event;
using IFramework.Infrastructure;
using IFramework.Message;

namespace Sample.DomainEvents
{
    [Topic("DomainEvent")]
    public class AggregateRootEvent : IAggregateRootEvent
    {
        public AggregateRootEvent()
        {
            Id = ObjectId.GenerateNewId().ToString();
        }

        public AggregateRootEvent(object aggregateRootId)
            : this()
        {
            AggregateRootId = aggregateRootId;
            Key = aggregateRootId.ToString();
        }

        public int Version { get; set; }

        public object AggregateRootId { get; }

        public string AggregateRootName { get; set; }

        public string Id { get; set; }

        public virtual string Key { get; set; }

        public  virtual string[] Tags { get; set; }
    }
}