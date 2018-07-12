using System;
using IFramework.Event;
using IFramework.Exceptions;
using Sample.DomainEvents;
using Sample.DomainEvents.Community;

namespace Sample.AsyncDomainEventSubscriber.Community
{
    public class AccountEventSubscriber :
        IEventSubscriber<AccountRegistered>
    {
        private readonly IEventBus _eventBus;

        public AccountEventSubscriber(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public void Handle(AccountRegistered @event)
        {
            Console.Write("subscriber1: {0} has registered.", @event.UserName);

            var applicationEvent = new ApplicationEvent.AccountRegistered
            {
                AccountID = new Guid(@event.AggregateRootId.ToString()),
                UserName = @event.UserName
            };
            _eventBus.Publish(applicationEvent);
        }
    }

    public class AccountEventSubscriber2 :
        IEventSubscriber<PeopleRegisted>,
        IEventSubscriber<AccountRegistered>
    {
        public void Handle(AccountRegistered @event)
        {
            Console.Write("subscriber2: {0} has registered.", @event.UserName);
            throw new DomainException(ErrorCode.UnknownError, "test fail handled event!");
        }

        public void Handle(PeopleRegisted message) { }
    }
}