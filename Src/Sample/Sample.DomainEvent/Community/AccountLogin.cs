using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFramework.Event;

namespace Sample.DomainEvent.Community
{
    public class AccountLogin : DomainEvent
    {
        public DateTime LoginTime { get; private set; }

        public AccountLogin(Guid accountID, DateTime loginTime)
            : base(accountID)
        {
            LoginTime = loginTime;
        }
    }
}
