using System;
using System.Collections.Generic;
using IFramework.Event;
using IFramework.Message;
using Sample.DomainEvents.Community;

namespace Sample.Domain.Model
{
    public class Account : People
    {
        protected Account()
        {
        }

        public Account(string username, string password, string email, Guid? id = null)
        {
            OnEvent(new AccountRegistered(id ?? Guid.NewGuid(), username,
                                          password, email, DateTime.Now));
        }

        public string Email { get; protected set; }
        public string Hoppy { get; protected set; }

        public virtual ICollection<ProductId> ProductIds { get; protected set; } = new HashSet<ProductId>();

        protected void Handle(AccountModified @event)
        {
            Email = @event.Email;
            RegisterTime = DateTime.Now;
        }

        void Handle(AccountRegistered @event)
        {
            base.Handle(@event);
            RegisterTime = @event.RegisterTime;
        }

        public void Modify(string email)
        {
            Profiles.Add(new AacountProfile(UserName, Email));
            OnEvent(new AccountModified(Id, email));
        }

        public virtual ICollection<AacountProfile> Profiles { get; protected set; } = new HashSet<AacountProfile>();
    }

    public class ProductId
    {
        public Guid Value { get; set; }

        /// <summary>
        /// </summary>
        public Guid AccountId { get; private set; }
    }
}