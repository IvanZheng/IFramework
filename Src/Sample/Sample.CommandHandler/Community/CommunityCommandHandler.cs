using IFramework.Command;
using IFramework.Infrastructure;
using IFramework.Repositories;
using IFramework.UnitOfWork;
using Sample.ApplicationEvent;
using Sample.Command;
using Sample.Domain.Model;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Text;

namespace Sample.CommandHandler.Community
{
    public class CommunityCommandHandler : CommandHandlerBase,
                                           ICommandHandler<Login>,
                                           ICommandHandler<Register>
    {
        /// <summary>
        /// Regard CommandHandler as a kind of application service,
        /// we do some query in it and also can publish some application event
        /// and no need to enter the domain layer!
        /// </summary>
        /// <param name="command"></param>
        public void Handle(Login command)
        {
            var account = DomainRepository.Find<Account>(a => a.UserName.Equals(command.UserName)
                                                                      && a.Password.Equals(command.Password));

            if (account == null)
            {
                throw new SysException(ErrorCode.WrongUsernameOrPassword);
            }

            EventPublisher.Publish(new AccountLogined { AccountID = account.ID, LoginTime = DateTime.Now });
            CommandContext.Reply = account.ID;
        }

        public void Handle(Register command)
        {
            if (DomainRepository.Find<Account>(a => a.UserName == command.UserName) != null)
            {
                throw new SysException(ErrorCode.UsernameAlreadyExists, string.Format("Username {0} exists!", command.UserName));
            }

            Account account = new Account(command.UserName, command.Password, command.Email);
            DomainRepository.Add(account);
            CommandContext.Reply = account.ID;
        }
    }
}
