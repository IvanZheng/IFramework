using IFramework.Event;
using IFramework.Infrastructure;

namespace IFramework.Test
{

    public class ApplicationUserEvent : IApplicationEvent
    {
        public string UserId { get; private set; }
        public string Name { get; private set; }
        public string Id { get; set; }
        public string Key { get; set; }
        public string[] Tags { get; set; }
        public string Topic { get; set; }


        public ApplicationUserEvent(string userId, string name)
        {
            UserId = userId;
            Name = name;
            Id = ObjectId.GenerateNewId().ToString();
        }
    }
    public class AggregateRootEvent : IAggregateRootEvent
    {
        public AggregateRootEvent(string aggregateRootId, int version)
        {
            Id = ObjectId.GenerateNewId().ToString();
            AggregateRootId = aggregateRootId;
            Version = version;
        }

        public string Id { get; set; }
        public string Key { get; set; }
        public string[] Tags { get; set; }
        public string Topic { get; set; }

        public object AggregateRootId { get; set; }
        public string AggregateRootName { get; set; }
        public int Version { get; set; }
    }

    public class UserCreated : AggregateRootEvent
    {
        public UserCreated(string aggregateRootId, string name, int version)
            : base(aggregateRootId, version)
        {
            Name = name;
        }

        public string Name { get; }
    }

    public class UserModified : AggregateRootEvent
    {
        public UserModified(string aggregateRootId, string name, int version)
            : base(aggregateRootId, version)
        {
            Name = name;
        }

        public string Name { get; }
    }
}