using IFramework.Command;
using IFramework.Event;
using Sample.ApplicationEvent;
using Sample.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sample.ApplicationEventSubscriber.Community
{
    public class AccountEventSubscriber : IEventSubscriber<AccountLogined>,
          IEventSubscriber<AccountRegistered>
    {
        IEventBus _eventBus;

        public AccountEventSubscriber(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }
        public void Handle(AccountLogined @event)
        {
            Console.Write("account({0}) logined at {1}", @event.AccountID, @event.LoginTime);
            var createProduct = new CreateProduct
            {
                ProductId = Guid.NewGuid(),
                Name = string.Format("{0}-{1}", DateTime.Now.ToString(), @event.ID),
                Count = 20000
            };
            _eventBus.SendCommand(createProduct);

        }

        public void Handle(AccountRegistered @event)
        {
            Console.Write("account({0}) registered at {1}", @event.AccountID, @event.UserName);
            _eventBus.SendCommand(new Login { UserName = "ivan", Password = "123456" });
        }
    }
}
