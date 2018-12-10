using System;
using System.Threading.Tasks;
using IFramework.Command;
using IFramework.DependencyInjection;
using IFramework.Event;
using IFramework.Exceptions;
using IFramework.Message;
using IFramework.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sample.ApplicationEvent;
using Sample.Command;
using Sample.Domain;
using Account = Sample.Domain.Model.Account;
using ErrorCode = Sample.DTO.ErrorCode;
using IFramework.MessageStores.MongoDb;

namespace Sample.CommandHandler.Community
{
    public class CommunityCommandHandler : ICommandAsyncHandler<Login>,
                                           ICommandHandler<Register>,
                                           ICommandHandler<Modify>
    {
        private readonly IMessageContext _commandContext;
        private readonly ILogger<CommunityCommandHandler> _logger;
        private readonly ICommunityRepository _domainRepository;
        private readonly IEventBus _eventBus;

        private readonly IUnitOfWork _unitOfWork;
        // private IContainer _container;

        public CommunityCommandHandler(IUnitOfWork unitOfWork,
                                       ICommunityRepository domainRepository,
                                       IEventBus eventBus,
                                       IMessageContext commanadContext,
                                       ILogger<CommunityCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _domainRepository = domainRepository;
            _commandContext = commanadContext;
            _logger = logger;
            _eventBus = eventBus;
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
            //var accounts = await _domainRepository.FindAll<Account>(a => a.UserName.Equals(command.UserName)
            //                                                              && a.Password.Equals(command.Password))
            //                                      .ToListAsync()
            //                                      .ConfigureAwait(false);
            _logger.LogDebug($"Handle Login command enter.");
            var account = await _domainRepository.FindAsync<Account>(a => a.UserName.Equals(command.UserName)
                                                                          && a.Password.Equals(command.Password))
                                                 .ConfigureAwait(false);
            if (account == null)
            {
                var ex = new DomainException(ErrorCode.WrongUsernameOrPassword,
                                             new[] {command.UserName});
                _eventBus.FinishSaga(ex);
                throw ex;
            }

            _eventBus.Publish(new AccountLogined {AccountId = account.Id, LoginTime = DateTime.Now, UserName = command.UserName, Tags = new []{command.UserName}});

            //await _UnitOfWork.CommitAsync()
            //                 .ConfigureAwait(false);
            _commandContext.Reply = account.Id;
        }

        public virtual void Handle(Modify command)
        {
            var account = _domainRepository.Find<Account>(a => a.UserName == command.UserName);
            if (account == null)
            {
                throw new DomainException(ErrorCode.UserNotExists);
            }
            account.Modify(command.Email);
            _unitOfWork.Commit();
            //_DomainRepository.Update(account);
        }

        public virtual void Handle(Register command)
        {
            if (_domainRepository.Find<Account>(a => a.UserName == command.UserName) != null)
            {
                throw new DomainException(ErrorCode.UsernameAlreadyExists,
                                          $"Username {command.UserName} exists!");
            }

            var account = new Account(command.UserName, command.Password, command.Email);
            _domainRepository.Add(account);
            _unitOfWork.Commit();
            _commandContext.Reply = account.Id;
        }
    }
}