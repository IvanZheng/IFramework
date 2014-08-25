using IFramework.Event;
using Sample.DomainEvents.Community;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sample.DomainEventHandler.Community
{
    public class AccountEventSubscriber : IEventSubscriber<AccountRegistered>
    {
        IEventBus _EventBus;
        public AccountEventSubscriber(IEventBus eventBus)
        {
            _EventBus = eventBus;
        }
        public void Handle(AccountRegistered @event)
        {
            Console.Write("send email to user.");

            // here is application event, not domain event
            _EventBus.Publish(new ApplicationEvent.AccountRegistered
                              {
                                  AccountID = (Guid)@event.AggregateRootID,
                                  UserName = @event.UserName
                              });
        }
    }
}
