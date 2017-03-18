using IFramework.Command;
using IFramework.Event;
using IFramework.Exceptions;
using Sample.DomainEvents;
using Sample.DomainEvents.Community;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Sample.AsyncDomainEventSubscriber.Community
{
    public class AccountEventSubscriber : 
        IEventSubscriber<AccountRegistered>,
        IEventSubscriber<SampleDomainException>
    {
        IEventBus _EventBus;
        public AccountEventSubscriber(IEventBus eventBus)
        {
            _EventBus = eventBus;
        }

        public void Handle(SampleDomainException message)
        {
            Console.Write($"{message.ErrorCode} {message.Message} {message.StackTrace}");
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

       
    }

    public class AccountEventSubscriber2:
        IEventSubscriber<PeopleRegisted>,
        IEventSubscriber<AccountRegistered>
    {
        public void Handle(PeopleRegisted message)
        {

        }

        public void Handle(AccountRegistered @event)
        {
            Console.Write("subscriber2: {0} has registered.", @event.UserName);
            throw new DomainException("test fail handled event!");

        }
    }

}
