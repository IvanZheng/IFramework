using IFramework.Domain;
using IFramework.Event;
using IFramework.Message;
using Sample.Command;
using Sample.DomainEvents.Community;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sample.Domain.Model
{
    public class Account : People,
        IEventSubscriber<AccountRegistered>,
        IEventSubscriber<AccountModified>
    {
        public string Email { get; private set; }
        public string Hoppy { get; set; }
        
        public virtual ICollection<ProductId> ProductIds { get; set; }

        //public byte[] Version { get; protected set; }

        public Account() {
            ProductIds = new HashSet<ProductId>();
        }

        public Account(string username, string password, string email)
        {
            ProductIds = new HashSet<ProductId>();
            OnEvent(new AccountRegistered(Guid.NewGuid(), username,
                                          password, email, DateTime.Now));
        }
        void IMessageHandler<AccountRegistered>.Handle(AccountRegistered @event)
        {
            (this as IMessageHandler<ItemRegisted>).Handle(@event);
            (this as IMessageHandler<PeopleRegisted>).Handle(@event);
            RegisterTime = @event.RegisterTime;
        }

        public void Modify(string email)
        {
            OnEvent(new AccountModified(ID, email));
        }
        void IMessageHandler<AccountModified>.Handle(AccountModified @event)
        {
            Email = @event.Email;
        }


    }

    public class ProductId
    {
        public Guid Value { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Guid AccountId { get; private set; }
    }
}
