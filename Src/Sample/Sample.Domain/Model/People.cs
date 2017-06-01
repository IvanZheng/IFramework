using System;
using IFramework.Domain;
using IFramework.Event;
using IFramework.Message;
using Sample.DomainEvents.Community;

namespace Sample.Domain.Model
{
    public abstract class People : VersionedAggregateRoot,
                                   IEventSubscriber<PeopleRegisted>,
                                   IEventSubscriber<ItemRegisted>
    {
        public People() { }

        public People(string username, string password, DateTime registerTime)
        {
            OnEvent(new PeopleRegisted(Guid.NewGuid(), username, password, registerTime));
        }

        public Guid ID { get; protected set; }
        public string UserName { get; protected set; }
        public string Password { get; protected set; }
        public DateTime RegisterTime { get; protected set; }

        void IMessageHandler<ItemRegisted>.Handle(ItemRegisted @event)
        {
            Console.Write(@event.ToString());
        }

        void IMessageHandler<PeopleRegisted>.Handle(PeopleRegisted @event)
        {
            ID = (Guid) @event.AggregateRootID;
            UserName = @event.UserName;
            Password = @event.Password;
            RegisterTime = @event.RegisterTime;
        }
    }
}