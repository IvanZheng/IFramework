using System;

namespace Sample.DomainEvents.Community
{
    public class ItemRegisted : AggregateRootEvent
    {
        public ItemRegisted(Guid id)
            : base(id) { }
    }

    public class PeopleRegisted : ItemRegisted
    {
        public PeopleRegisted(Guid accountID, string username, string password, DateTime registerTime)
            : base(accountID)
        {
            UserName = username;
            Password = password;
            RegisterTime = registerTime;
        }

        public string UserName { get; }
        public string Password { get; }
        public DateTime RegisterTime { get; }
    }

    public class AccountRegistered : PeopleRegisted
    {
        public AccountRegistered(Guid accountID,
                                 string username,
                                 string password,
                                 string email,
                                 DateTime registerTime)
            : base(accountID, username, password, registerTime)
        {
            Email = email;
        }

        public string Email { get; }
    }

    public class AccountModified : AggregateRootEvent
    {
        public AccountModified(Guid accountId, string email)
            : base(accountId)
        {
            Email = email;
        }

        public string Email { get; }
    }
}