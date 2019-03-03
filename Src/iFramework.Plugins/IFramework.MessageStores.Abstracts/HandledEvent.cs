using System;
using System.ComponentModel.DataAnnotations;

namespace IFramework.MessageStores.Abstracts
{
    public class HandledEvent
    {
        public HandledEvent() { }

        public HandledEvent(string id, string subscriptionName, string topic, DateTime handledTime)
        {
            Id = id;
            SubscriptionName = subscriptionName;
            Topic = topic;
            HandledTime = handledTime;
        }

        [MaxLength(50)]
        public string Id { get; set; }
        public string SubscriptionName { get; set; }
        public DateTime HandledTime { get; set; }
        public string Topic { get; set; }
    }

    public class FailHandledEvent : HandledEvent
    {
        public FailHandledEvent() { }

        public FailHandledEvent(string id, string subscriptionName, string topic, DateTime handledTime, Exception e)
            : base(id, subscriptionName, topic, handledTime)
        {
            Error = e.GetBaseException().Message;
            StackTrace = e.StackTrace;
        }

        public string Error { get; set; }
        public string StackTrace { get; set; }
    }
}