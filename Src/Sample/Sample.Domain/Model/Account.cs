using System;
using System.Collections.Generic;
using IFramework.Event;
using IFramework.Message;
using Sample.DomainEvents.Community;

namespace Sample.Domain.Model
{
    public class Account : People,
                           IEventSubscriber<AccountRegistered>,
                           IEventSubscriber<AccountModified>
    {
        protected Account()
        {
        }

        public Account(string username, string password, string email)
        {
            OnEvent(new AccountRegistered(Guid.NewGuid(), username,
                                          password, email, DateTime.Now));
        }

        public string Email { get; protected set; }
        public string Hoppy { get; protected set; }

        public virtual ICollection<ProductId> ProductIds { get; protected set; } = new HashSet<ProductId>();

        void IMessageHandler<AccountModified>.Handle(AccountModified @event)
        {
            Email = @event.Email;
        }

        void IMessageHandler<AccountRegistered>.Handle(AccountRegistered @event)
        {
            (this as IMessageHandler<ItemRegisted>).Handle(@event);
            (this as IMessageHandler<PeopleRegisted>).Handle(@event);
            RegisterTime = @event.RegisterTime;
        }

        public void Modify(string email)
        {
            OnEvent(new AccountModified(Id, email));
        }
    }

    public class ProductId
    {
        public Guid Value { get; set; }

        /// <summary>
        /// </summary>
        public Guid AccountId { get; private set; }
    }
}