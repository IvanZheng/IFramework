using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace IFramework.MessageStores.MongoDb
{
    [BsonKnownTypes(typeof(HandledEvent), typeof(FailHandledEvent))]
    [BsonDiscriminator(RootClass = true)]
    public class HandledEventBase: Abstracts.HandledEvent
    {
        public string EventId { get; set; }
        public HandledEventBase() { }

        public HandledEventBase(string id, string subscriptionName, DateTime handledTime) 
            : base(id, subscriptionName, handledTime)
        {
            EventId = id;
            Id = $"{EventId}_{subscriptionName}";
        }
    }

    [BsonDiscriminator(nameof(HandledEvent))]
    public class HandledEvent : HandledEventBase
    {
        public HandledEvent() { }
        public HandledEvent(string id, string subscriptionName, DateTime handledTime) 
            : base(id, subscriptionName, handledTime) { }
    }

    [BsonDiscriminator(nameof(FailHandledEvent))]
    public class FailHandledEvent : HandledEventBase
    {
        public FailHandledEvent() { }

        public FailHandledEvent(string id, string subscriptionName, DateTime handledTime, Exception e)
            : base(id, subscriptionName, handledTime)
        {
            Error = e.GetBaseException().Message;
            StackTrace = e.StackTrace;
        }

        public string Error { get; set; }
        public string StackTrace { get; set; }
    }

}
