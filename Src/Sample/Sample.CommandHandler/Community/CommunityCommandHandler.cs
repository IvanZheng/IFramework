using System;
using System.Threading.Tasks;
using IFramework.Command;
using IFramework.Event;
using IFramework.IoC;
using IFramework.Message;
using IFramework.UnitOfWork;
using Sample.ApplicationEvent;
using Sample.Command;
using Sample.Domain;
using Sample.DomainEvents;
using Sample.DTO;
using Sample.Persistence.Repositories;
using Account = Sample.Domain.Model.Account;

namespace Sample.CommandHandler.Community
{
    public class CommunityCommandHandler : ICommandAsyncHandler<Login>,
                                           ICommandHandler<Register>,
                                           ICommandHandler<Modify>
    {
        private readonly IMessageContext _CommandContext;
        private readonly ICommunityRepository _DomainRepository;
        private readonly IEventBus _EventBus;
        private readonly IUnitOfWork _UnitOfWork;
       // private IContainer _container;

        public CommunityCommandHandler(IUnitOfWork unitOfWork,
                                       ICommunityRepository domainRepository,
                                       IEventBus eventBus,
                                       IMessageContext commandContext
                                       )
        {
            _UnitOfWork = unitOfWork;
            _DomainRepository = domainRepository;
            _CommandContext = commandContext;
            _EventBus = eventBus;
           // _container = container;
        }

        /// <summary>
        ///     Regard CommandHandler as a kind of application service,
        ///     we do some query in it and also can publish some application event
        ///     and no need to enter the domain layer!
        /// </summary>
        /// <param name="command"></param>
        public virtual async Task Handle(Login command)
        {
            var account = await _DomainRepository.FindAsync<Account>(a => a.UserName.Equals(command.UserName)
                                                                          && a.Password.Equals(command.Password))
                                                 .ConfigureAwait(false);
            if (account == null)
            {
                var ex = new SampleDomainException(ErrorCode.WrongUsernameOrPassword,
                                                   new[] {command.UserName});
                _EventBus.FinishSaga(ex);
                throw ex;
            }

            _EventBus.Publish(new AccountLogined {AccountID = account.ID, LoginTime = DateTime.Now}); 

            //await _UnitOfWork.CommitAsync()
            //                 .ConfigureAwait(false);
            _CommandContext.Reply = account.ID;
        }

        public virtual void Handle(Modify command)
        {
            var account = _DomainRepository.Find<Account>(a => a.UserName == command.UserName);
            if (account == null)
            {
                throw new SampleDomainException(ErrorCode.UserNotExists);
            }
            account.Modify(command.Email);
            _UnitOfWork.Commit();
            //_DomainRepository.Update(account);
        }

        public virtual void Handle(Register command)
        {
            if (_DomainRepository.Find<Account>(a => a.UserName == command.UserName) != null)
            {
                throw new SampleDomainException(ErrorCode.UsernameAlreadyExists,
                                                string.Format("Username {0} exists!", command.UserName));
            }

            var account = new Account(command.UserName, command.Password, command.Email);
            _DomainRepository.Add(account);
            _UnitOfWork.Commit();
            _CommandContext.Reply = account.ID;
        }
    }
}