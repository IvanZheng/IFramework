using System;
using IFramework.Event;
using Sample.ApplicationEvent;
using Sample.Command;

namespace Sample.ApplicationEventSubscriber.Community
{
    public class AccountEventSubscriber : IEventSubscriber<AccountLogined>,
                                          IEventSubscriber<AccountRegistered>
    {
        private readonly IEventBus _eventBus;

        public AccountEventSubscriber(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public void Handle(AccountLogined @event)
        {
            Console.Write("account({0}) logined at {1}", @event.AccountId, @event.LoginTime);
            var createProduct = new CreateProduct
            {
                ProductId = Guid.NewGuid(),
                Name = string.Format("{0}-{1}", DateTime.Now, @event.Id),
                Count = 20000
            };
            _eventBus.SendCommand(createProduct);
        }

        public void Handle(AccountRegistered @event)
        {
            Console.Write("account({0}) registered at {1}", @event.AccountID, @event.UserName);
            _eventBus.SendCommand(new Login {UserName = "ivan", Password = "123456"});
        }
    }
}