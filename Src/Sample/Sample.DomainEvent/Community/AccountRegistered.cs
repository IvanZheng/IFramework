using IFramework.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sample.DomainEvent.Community
{
    public class AccountRegistered : DomainEvent
    {
        public string UserName { get;  private set; }
        public string Password { get; private set; }
        public string Email { get; private set; }
        public DateTime RegisterTime { get; private set; }

        public AccountRegistered(Guid accountID, string username, string password,
                                 string email, DateTime registerTime)
            : base(accountID)
        {
            UserName = username;
            Password = password;
            Email = email;
            RegisterTime = registerTime;
        }
    }
}
