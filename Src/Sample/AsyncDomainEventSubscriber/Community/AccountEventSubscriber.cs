using System;
using IFramework.Event;
using IFramework.Exceptions;
using Sample.DomainEvents;
using Sample.DomainEvents.Community;

namespace Sample.AsyncDomainEventSubscriber.Community
{
    public class AccountEventSubscriber :
        IEventSubscriber<AccountRegistered>,
        IEventSubscriber<SampleDomainException>
    {
        private readonly IEventBus _EventBus;

        public AccountEventSubscriber(IEventBus eventBus)
        {
            _EventBus = eventBus;
        }

        public void Handle(AccountRegistered @event)
        {
            Console.Write("subscriber1: {0} has registered.", @event.UserName);

            var applicationEvent = new ApplicationEvent.AccountRegistered
            {
                AccountID = new Guid(@event.AggregateRootID.ToString()),
                UserName = @event.UserName
            };
            _EventBus.Publish(applicationEvent);
        }

        public void Handle(SampleDomainException message)
        {
            Console.Write($"{message.ErrorCode} {message.Message} {message.StackTrace}");
        }
    }

    public class AccountEventSubscriber2 :
        IEventSubscriber<PeopleRegisted>,
        IEventSubscriber<AccountRegistered>
    {
        public void Handle(AccountRegistered @event)
        {
            Console.Write("subscriber2: {0} has registered.", @event.UserName);
            throw new DomainException("test fail handled event!");
        }

        public void Handle(PeopleRegisted message) { }
    }
}