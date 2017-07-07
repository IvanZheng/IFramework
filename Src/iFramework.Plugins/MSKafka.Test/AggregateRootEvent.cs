using IFramework.Event;
using IFramework.Infrastructure;

namespace KafkaClient.Test
{
    public class AggregateRootEvent : IAggregateRootEvent
    {
        public AggregateRootEvent() { }

        public AggregateRootEvent(string body)
        {
            Body = body;
            ID = ObjectId.GenerateNewId().ToString();
        }


        public AggregateRootEvent(object aggregateRootID, string body)
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