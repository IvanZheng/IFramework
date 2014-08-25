using IFramework.Event;
using Sample.ApplicationEvent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sample.ApplicationEventSubscriber.Community
{
    public class AccountEventSubscriber : IEventSubscriber<AccountLogined>,
        IEventSubscriber<AccountRegistered>
    {
        public void Handle(AccountLogined @event)
        {
            Console.Write("account({0}) logined at {1}", @event.AccountID, @event.LoginTime);
        }

        public void Handle(AccountRegistered @event)
        {
            Console.Write("account({0}) registered at {1}", @event.AccountID, @event.UserName);

        }
    }
}
