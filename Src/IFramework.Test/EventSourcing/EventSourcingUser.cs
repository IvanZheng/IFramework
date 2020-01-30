using System;
using System.Collections.Generic;
using System.Text;
using IFramework.Event;
using IFramework.Infrastructure.EventSourcing.Domain;
using IFramework.Message;

namespace IFramework.Test.EventSourcing
{
    public class EventSourcingUser: EventSourcingAggregateRoot
    {
        public string Name { get; private set; }
        void Handle(UserCreated message)
        {
            Id = message.AggregateRootId.ToString();
            Name = message.Name;
        }

        void Handle(UserModified message)
        {
            Name = message.Name;
        }
    }
}
