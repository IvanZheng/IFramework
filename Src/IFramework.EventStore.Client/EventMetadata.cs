using IFramework.Event;
using Newtonsoft.Json;

namespace IFramework.EventStore.Client
{
    public class EventMetadata
    {
        public string Id { get; set; }
        public string Key { get; set; }
        public long Version { get; set; }
        public string Type { get; set; }
        public string CorrelationId { get;set;}
        public EventMetadata(){}

        public EventMetadata(IEvent @event, long version, string type, string correlationId)
            :this(@event.Id, @event.Key, version, type, correlationId)
        {
            
        }

        public EventMetadata(string id, string key, long version, string type, string correlationId)
        {
            Id = id;
            Key = key;
            Version = version;
            Type = type;
            CorrelationId = correlationId;
        }
    }
}
