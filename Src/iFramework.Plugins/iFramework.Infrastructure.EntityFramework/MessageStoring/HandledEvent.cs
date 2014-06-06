using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;

namespace IFramework.EntityFramework.MessageStoring
{
    public class HandledEvent
    {
        public string Id { get; set; }
        public string SubscriptionName { get; set; }
        public HandledEvent() { }
        public HandledEvent(string id, string subscriptionName)
        {
            Id = id;
            SubscriptionName = subscriptionName;
        }
    }
}
