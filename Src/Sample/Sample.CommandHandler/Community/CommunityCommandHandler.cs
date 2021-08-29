using System;
using System.Threading;
using System.Threading.Tasks;
using IFramework.Command;
using IFramework.DependencyInjection;
using IFramework.Event;
using IFramework.Exceptions;
using IFramework.Message;
using IFramework.UnitOfWork;
using Microsoft.Extensions.Logging;
using Sample.ApplicationEvent;
using Sample.Command;
using Sample.Command.Community;
using Sample.Domain;
using Account = Sample.Domain.Model.Account;
using ErrorCode = Sample.DTO.ErrorCode;

namespace Sample.CommandHandler.Community
{
    public class CommunityCommandHandler : ICommandAsyncHandler<Login>,
                                           ICommandHandler<Register>,
                                           ICommandAsyncHandler<Modify>,
                                           ICommandAsyncHandler<CommonCommand>
    {
        private const string IxAccountsEmail = "IX_Accounts_Email";
        private readonly IMessageContext _commandContext;
        private readonly ILogger<CommunityCommandHandler> _logger;
        private readonly ICommunityRepository _domainRepository;
        private readonly IEventBus _eventBus;

        private readonly IUnitOfWork _unitOfWork;
        // private IContainer _container;

        public CommunityCommandHandler(IUnitOfWork unitOfWork,
                                       ICommunityRepository domainRepository,
                                       IEventBus eventBus,
                                       IMessageContext commandContext,
                                       ILogger<CommunityCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _domainRepository = domainRepository;
            _commandContext = commandContext;
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
        /// <param name="cancellationToken"></param>
        public virtual async Task Handle(Login command, CancellationToken cancellationToken)
        {
            //_logger.LogDebug($"Handle Login command enter.");
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

        [ConcurrentProcess(IxAccountsEmail)]
        public virtual async Task Handle(Modify command, CancellationToken cancellationToken)
        {
            var account = await _domainRepository.FindAsync<Account>(a => a.UserName == command.UserName);
            if (account == null)
            {
                throw new DomainException(ErrorCode.UserNotExists);
            }

            var emailExists = await _domainRepository.ExistsAsync<Account>(a => a.Email == command.Email && a.Id != account.Id);

            account.Modify(emailExists ? $"{command.Email}.{DateTime.Now.Ticks}" : command.Email);
            await _unitOfWork.CommitAsync(cancellationToken);
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

        public Task Handle(CommonCommand message, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}