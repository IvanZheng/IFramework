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

        
        //public byte[] Version { get; protected set; }

        public Account() { }

        public Account(string username, string password, string email)
        {
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
}
