using IFramework.Event;
using IFramework.Infrastructure;

namespace MSKafka.Test
{
    public class DomainEvent : IDomainEvent
    {
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
            Key = aggregateRootID.ToString();
        }

        public string Body { get; set; }
        public int Version { get; set; }

        public object AggregateRootID { get; }

        public virtual string Key { get; set; }

        public string AggregateRootName { get; set; }

        public string ID { get; set; }
    }
}