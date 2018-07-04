using System;

namespace IFramework.MessageStores.Relational
{
    public class HandledEvent
    {
        public HandledEvent() { }

        public HandledEvent(string id, string subscriptionName, DateTime handledTime)
        {
            Id = id;
            SubscriptionName = subscriptionName;
            HandledTime = handledTime;
        }

        public string Id { get; set; }
        public string SubscriptionName { get; set; }
        public DateTime HandledTime { get; set; }
    }

    public class FailHandledEvent : HandledEvent
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