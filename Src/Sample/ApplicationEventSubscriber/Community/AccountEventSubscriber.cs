using System;
using System.Threading.Tasks;
using IFramework.Event;
using IFramework.Exceptions;
using IFramework.Infrastructure;
using IFramework.UnitOfWork;
using Sample.ApplicationEvent;
using Sample.Command;
using Sample.Domain;
using Sample.Domain.Model;
using ErrorCode = Sample.DTO.ErrorCode;

namespace Sample.ApplicationEventSubscriber.Community
{
    public class AccountEventSubscriber : IEventAsyncSubscriber<AccountLogined>,
                                          IEventSubscriber<AccountRegistered>
    {
        private readonly IEventBus _eventBus;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICommunityRepository _domainRepository;
        private readonly IConcurrencyProcessor _concurrencyProcessor;

        public AccountEventSubscriber(IEventBus eventBus, IUnitOfWork unitOfWork,
                                      ICommunityRepository domainRepository,
                                      IConcurrencyProcessor concurrencyProcessor)
        {
            _eventBus = eventBus;
            _unitOfWork = unitOfWork;
            _domainRepository = domainRepository;
            _concurrencyProcessor = concurrencyProcessor;
        }

        public async Task Handle(AccountLogined @event)
        {
            //await _concurrencyProcessor.ProcessAsync(async () =>
            //{
            //    var account = await _domainRepository.FindAsync<Account>(a => a.UserName == @event.UserName);
            //    if (account == null)
            //    {
            //        throw new DomainException(ErrorCode.UserNotExists);
            //    }
            //    account.Modify("ivan@163.com");
            //    await _unitOfWork.CommitAsync();
            //});

            //await _concurrencyProcessor.ProcessAsync(async () =>
            //{
            //    var account = await _domainRepository.FindAsync<Account>(a => a.UserName == "ivan1");
            //    if (account == null)
            //    {
            //        throw new DomainException(ErrorCode.UserNotExists);
            //    }
            //    account.Modify("ivan1@163.com");
            //    await _unitOfWork.CommitAsync();
            //});
            Console.Write("account({0}) logined at {1}", @event.AccountId, @event.LoginTime);
            var createProduct = new CreateProduct
            {
                ProductId = Guid.NewGuid(),
                Name = $"{DateTime.Now}-{@event.Id}",
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