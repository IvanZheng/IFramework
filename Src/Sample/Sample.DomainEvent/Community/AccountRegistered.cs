using System;

namespace Sample.DomainEvents.Community
{
    public class ItemRegisted : AggregateRootEvent
    {
        public ItemRegisted(Guid aggregateRootId)
            : base(aggregateRootId) { }
    }

    public class PeopleRegisted : ItemRegisted
    {
        public PeopleRegisted(Guid aggregateRootId, string username, string password, DateTime registerTime)
            : base(aggregateRootId)
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
        public AccountRegistered(Guid aggregateRootId,
                                 string username,
                                 string password,
                                 string email,
                                 DateTime registerTime)
            : base(aggregateRootId, username, password, registerTime)
        {
            Email = email;
        }

        public string Email { get; }
    }

    public class AccountModified : AggregateRootEvent
    {
        public AccountModified(Guid aggregateRootId, string email)
            : base(aggregateRootId)
        {
            Email = email;
        }

        public string Email { get; }
    }
}