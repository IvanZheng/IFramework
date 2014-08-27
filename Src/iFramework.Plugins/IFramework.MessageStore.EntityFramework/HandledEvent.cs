using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;

namespace IFramework.MessageStoring
{
    public class HandledEvent
    {
        public string Id { get; set; }
        [MaxLength(200)]
        public string SubscriptionName { get; set; }
        public DateTime HandledTime { get; set; }

        public HandledEvent() { }
        public HandledEvent(string id, string subscriptionName, DateTime handledTime)
        {
            Id = id;
            SubscriptionName = subscriptionName;
            HandledTime = handledTime;
        }
    }

    public class FailHandledEvent : HandledEvent
    {
        public string Error { get; set; }
        public string StackTrace { get; set; }
        public FailHandledEvent() { }
        public FailHandledEvent(string id, string subscriptionName, DateTime handledTime, Exception e)
            :base(id, subscriptionName, handledTime)
        {
            Error = e.GetBaseException().Message;
            StackTrace = e.StackTrace;
        }
    }
}
