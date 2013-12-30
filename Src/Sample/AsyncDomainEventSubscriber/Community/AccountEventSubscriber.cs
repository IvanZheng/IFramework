using IFramework.Event;
using Sample.DomainEvent.Community;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Sample.AsyncDomainEventSubscriber.Community
{
    public class AccountEventSubscriber : IEventSubscriber<AccountRegistered>,
                                          IEventSubscriber<AccountLogin>
    {
        public void Handle(AccountRegistered @event)
        {
            //Console.Write("{0} has registered.", @event.UserName);
        }

        public void Handle(AccountLogin @event)
        {
            //Console.Write("{0} has login.", @event.AccountID);
        }
    }
}
