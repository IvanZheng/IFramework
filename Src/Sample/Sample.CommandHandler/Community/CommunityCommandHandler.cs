using IFramework.Command;
using IFramework.Event;
using IFramework.Infrastructure;
using IFramework.Message;
using IFramework.Repositories;
using IFramework.UnitOfWork;
using Sample.ApplicationEvent;
using Sample.Command;
using Sample.Domain.Model;
using Sample.Persistence.Repositories;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Text;

namespace Sample.CommandHandler.Community
{
    public class CommunityCommandHandler : ICommandHandler<Login>,
                                           ICommandHandler<Register>,       
                                           ICommandHandler<Modify>
    {
        IEventPublisher _EventPublisher;
        IMessageContext _CommandContext;
        CommunityRepository _DomainRepository;
        IUnitOfWork _UnitOfWork;
        public CommunityCommandHandler(IUnitOfWork unitOfWork, 
                                       CommunityRepository domainRepository,
                                       IEventPublisher eventPublisher,
                                       IMessageContext commandContext)
        {
            _UnitOfWork = unitOfWork;
            _DomainRepository = domainRepository;
            _CommandContext = commandContext;
            _EventPublisher = eventPublisher;
        }

        /// <summary>
        /// Regard CommandHandler as a kind of application service,
        /// we do some query in it and also can publish some application event
        /// and no need to enter the domain layer!
        /// </summary>
        /// <param name="command"></param>
        public void Handle(Login command)
        {
            var account = _DomainRepository.Find<Account>(a => a.UserName.Equals(command.UserName)
                                                                      && a.Password.Equals(command.Password));
            if (account == null)
            {
                throw new SysException(ErrorCode.WrongUsernameOrPassword);
            }
            
            _EventPublisher.Publish(new AccountLogined { AccountID = account.ID, LoginTime = DateTime.Now });
            
            _CommandContext.Reply = account.ID;
        }

        public void Handle(Register command)
        {
            if (_DomainRepository.Find<Account>(a => a.UserName == command.UserName) != null)
            {
                throw new SysException(ErrorCode.UsernameAlreadyExists, string.Format("Username {0} exists!", command.UserName));
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
                throw new SysException(ErrorCode.UserNotExists);
            }
            account.Modify(command.Email);
            //_DomainRepository.Update(account);
        }
    }
}
