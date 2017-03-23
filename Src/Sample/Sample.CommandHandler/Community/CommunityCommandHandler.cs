using IFramework.Command;
using IFramework.Event;
using IFramework.Infrastructure;
using IFramework.IoC;
using IFramework.Message;
using IFramework.Repositories;
using IFramework.Exceptions;
using IFramework.UnitOfWork;
using Sample.ApplicationEvent;
using Sample.Command;
using Sample.Domain.Model;
using Sample.Persistence.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sample.DomainEvents;

namespace Sample.CommandHandler.Community
{
    public class CommunityCommandHandler : ICommandAsyncHandler<Login>,
                                           ICommandHandler<Register>,
                                           ICommandHandler<Modify>
    {
        IEventBus _EventBus;
        IMessageContext _CommandContext;
        CommunityRepository _DomainRepository;
        IUnitOfWork _UnitOfWork;
        IContainer _container;
        public CommunityCommandHandler(IUnitOfWork unitOfWork,
                                       CommunityRepository domainRepository,
                                       IEventBus eventBus,
                                       IMessageContext commandContext,
                                       IContainer container)
        {
            _UnitOfWork = unitOfWork;
            _DomainRepository = domainRepository;
            _CommandContext = commandContext;
            _EventBus = eventBus;
            _container = container;
        }

        /// <summary>
        /// Regard CommandHandler as a kind of application service,
        /// we do some query in it and also can publish some application event
        /// and no need to enter the domain layer!
        /// </summary>
        /// <param name="command"></param>
        public async Task Handle(Login command)
        {
            var account = await _DomainRepository.FindAsync<Account>(a => a.UserName.Equals(command.UserName)
                                                            && a.Password.Equals(command.Password))
                                                 .ConfigureAwait(false);
            if (account == null)
            {
                var ex = new SampleDomainException(DTO.ErrorCode.WrongUsernameOrPassword,
                                                new[] { command.UserName });
                _EventBus.FinishSaga(ex);
                throw ex;
            }

            _EventBus.Publish(new AccountLogined { AccountID = account.ID, LoginTime = DateTime.Now });
            await _UnitOfWork.CommitAsync()
                             .ConfigureAwait(false);
            _CommandContext.Reply = account.ID;
        }

        public void Handle(Register command)
        {
            if (_DomainRepository.Find<Account>(a => a.UserName == command.UserName) != null)
            {
                throw new SampleDomainException(DTO.ErrorCode.UsernameAlreadyExists, string.Format("Username {0} exists!", command.UserName));
            }

            Account account = new Account(command.UserName, command.Password, command.Email);
            _DomainRepository.Add(account);
            _UnitOfWork.Commit();
            _CommandContext.Reply = account.ID;
        }

        public void Handle(Modify command)
        {
            var account = _DomainRepository.Find<Account>(a => a.UserName == command.UserName);
            if (account == null)
            {
                throw new SampleDomainException(DTO.ErrorCode.UserNotExists);
            }
            account.Modify(command.Email);
            _UnitOfWork.Commit();
            //_DomainRepository.Update(account);
        }
    }
}
