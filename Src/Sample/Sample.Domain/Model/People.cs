using IFramework.Domain;
using IFramework.Event;
using Sample.DomainEvents.Community;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sample.Domain.Model
{
    public abstract class People : TimestampedAggregateRoot,
        IEventSubscriber<PeopleRegisted>,
        IEventSubscriber<ItemRegisted>
    {
        public Guid ID { get; protected set; }
        public string UserName { get; protected set; }
        public string Password { get; protected set; }
        public DateTime RegisterTime { get; protected set; }

        public People() { }

        public People(string username, string password, DateTime registerTime)
        {
            OnEvent(new PeopleRegisted(Guid.NewGuid(), username, password, registerTime));
        }
        void IFramework.Message.IMessageHandler<PeopleRegisted>.Handle(PeopleRegisted @event)
        {
            ID = (Guid)@event.AggregateRootID;
            UserName = @event.UserName;
            Password = @event.Password;
            RegisterTime = @event.RegisterTime;
        }

        void IFramework.Message.IMessageHandler<ItemRegisted>.Handle(ItemRegisted @event)
        {
            Console.Write(@event.ToString());
        }
    }
}
