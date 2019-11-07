using System;
using System.Collections.Generic;
using System.Text;
using IFramework.Event;

namespace IFramework.EventStore
{
    public class EventMetadata
    {
        public string Id { get; set; }
        public string Key { get; set; }
        public long Version { get; set; }
        public string Type { get; set; }

        public EventMetadata(){}

        public EventMetadata(IEvent @event, long version, string type)
            :this(@event.Id, @event.Key, version, type)
        {
            
        }

        public EventMetadata(string id, string key, long version, string type)
        {
            Id = id;
            Key = key;
            Version = version;
            Type = type;
        }
    }
}
