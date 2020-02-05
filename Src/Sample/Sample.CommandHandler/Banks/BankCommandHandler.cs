using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using IFramework.Exceptions;
using IFramework.Infrastructure.EventSourcing.Domain;
using IFramework.Infrastructure.EventSourcing.Repositories;
using IFramework.Message;
using Sample.Domain.Model.Bank.Accounts;
using ErrorCode = Sample.DTO.ErrorCode;

namespace Sample.CommandHandler.Banks
{
    public abstract class BankCommandHandler<TAggregateRoot> where TAggregateRoot: class, IEventSourcingAggregateRoot
    {
        protected readonly IMessageContext CommandContext;
        protected readonly IEventSourcingRepository<TAggregateRoot> Repository;
        protected BankCommandHandler(IMessageContext commandContext, IEventSourcingRepository<TAggregateRoot> repository)
        {
            CommandContext = commandContext;
            Repository = repository;
        }

        protected async Task<TAggregateRoot> GetAggregateRootAsync(string id, bool throwExceptionIfNotExists = true)
        {
            var aggregateRoot = await Repository.GetByKeyAsync(id)
                                           .ConfigureAwait(false);
            if (aggregateRoot == null && throwExceptionIfNotExists)
            {
                throw new DomainException(ErrorCode.ObjectNotExists, new object[] {id});
            }

            return aggregateRoot;
        }
    }
}
