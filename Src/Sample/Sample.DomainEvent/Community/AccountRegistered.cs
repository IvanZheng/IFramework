using IFramework.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sample.DomainEvent.Community
{
    public class ItemRegisted: DomainEvent
    {
        public ItemRegisted(Guid id)
            :base(id)
        {

        }
    }

    public class PeopleRegisted : ItemRegisted
    {
        public string UserName { get; private set; }
        public string Password { get; private set; }
        public DateTime RegisterTime { get; private set; }

        public PeopleRegisted(Guid accountID, string username, string password, DateTime registerTime)
            : base(accountID)
        {
            UserName = username;
            Password = password;
            RegisterTime = registerTime;
        }
    }
    public class AccountRegistered : PeopleRegisted
    {
        public string Email { get; private set; }

        public AccountRegistered(Guid accountID, string username, string password,
                                 string email, DateTime registerTime)
            : base(accountID, username, password, registerTime)
        {
            Email = email;
        }
    }

    public class AccountModified : DomainEvent
    {
        public string Email { get; private set; }
        public AccountModified(Guid id, string email)
            :base(id)
        {
            Email = email;
        }
    }
}
