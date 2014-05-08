using IFramework.Event;
using Sample.DomainEvents.Community;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Sample.AsyncDomainEventSubscriber.Community
{
    public class AccountEventSubscriber : 
        IEventSubscriber<AccountRegistered>
    {
        public void Handle(AccountRegistered @event)
        {
            //Console.Write("{0} has registered.", @event.UserName);
        }

       
    }

    public class AccountEventSubscriber2:
        IEventSubscriber<PeopleRegisted>
    {
        public void Handle(PeopleRegisted message)
        {

        }
    }

}
