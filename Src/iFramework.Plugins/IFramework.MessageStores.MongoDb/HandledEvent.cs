using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;
using IFramework.MessageQueue;

namespace IFramework.MessageStores.MongoDb
{
    [BsonKnownTypes(typeof(HandledEvent), typeof(FailHandledEvent))]
    [BsonDiscriminator(RootClass = true)]
    public class HandledEventBase: Abstracts.HandledEvent
    {
        public string EventId { get; set; }
        public HandledEventBase() { }

        public HandledEventBase(string id, string subscriptionName, MessageOffset messageOffset, DateTime handledTime) 
            : base(id, subscriptionName, messageOffset, handledTime)
        {
            EventId = id;
            Id = $"{EventId}_{subscriptionName}";
        }
    }

    [BsonDiscriminator(nameof(HandledEvent))]
    public class HandledEvent : HandledEventBase
    {
        public HandledEvent() { }
        public HandledEvent(string id, string subscriptionName, MessageOffset messageOffset, DateTime handledTime) 
            : base(id, subscriptionName, messageOffset, handledTime) { }
    }

    [BsonDiscriminator(nameof(FailHandledEvent))]
    public class FailHandledEvent : HandledEventBase
    {
        public FailHandledEvent() { }

        public FailHandledEvent(string id, string subscriptionName, MessageOffset messageOffset, DateTime handledTime, Exception e)
            : base(id, subscriptionName, messageOffset, handledTime)
        {
            Error = e.GetBaseException().Message;
            StackTrace = e.StackTrace;
        }

        public string Error { get; set; }
        public string StackTrace { get; set; }
    }

}
