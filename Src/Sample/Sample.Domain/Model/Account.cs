using IFramework.Domain;
using IFramework.Event;
using IFramework.Message;
using Sample.Command;
using Sample.DomainEvent.Community;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sample.Domain.Model
{
    public class Account : AggregateRoot,
                           IEventSubscriber<AccountRegistered>
    {
        public Guid ID { get; private set; }
        public string UserName { get; private set; }
        public string Password { get; private set; }
        public DateTime RegisterTime { get; private set; }

        public Account() { }
        public Account(string username, string password, string email)
        {
            OnEvent(new AccountRegistered(Guid.NewGuid(), username,
                                          password, email, DateTime.Now));
        }
        void IMessageHandler<AccountRegistered>.Handle(AccountRegistered @event)
        {
            {
                ID = (Guid)@event.AggregateRootID;
                UserName = @event.UserName;
                Password = @event.Password;
                RegisterTime = @event.RegisterTime;
            }
        }
    }
}
