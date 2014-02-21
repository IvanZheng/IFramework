using IFramework.Event;
using Sample.DomainEvent.Community;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sample.DomainEventHandler.Community
{
    public class AccountEventSubscriber : IEventSubscriber<AccountRegistered>
    {
        public void Handle(AccountRegistered @event)
        {
            Console.Write("send email to user.");
        }
    }
}
