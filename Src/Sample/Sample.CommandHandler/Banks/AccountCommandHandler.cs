using System;
using System.Threading.Tasks;
using IFramework.Command;
using IFramework.Infrastructure.EventSourcing.Repositories;
using IFramework.Message;
using Sample.Command.Banks;
using Sample.Domain.Model.BankAccounts;

namespace Sample.CommandHandler.Banks
{
    public class AccountCommandHandler : ICommandAsyncHandler<CreateAccount>,
                                         ICommandAsyncHandler<PrepareAccountCredit>,
                                         ICommandAsyncHandler<PrepareAccountDebit>,
                                         ICommandAsyncHandler<CommitAccountCredit>,
                                         ICommandAsyncHandler<CommitAccountDebit>
    {
        private readonly IMessageContext _commandContext;
        private readonly IEventSourcingRepository<BankAccount> _repository;

        public AccountCommandHandler(IMessageContext commandContext, IEventSourcingRepository<BankAccount> repository)
        {
            _commandContext = commandContext;
            _repository = repository;
        }

        public Task Handle(CommitAccountCredit message)
        {
            throw new NotImplementedException();
        }

        public Task Handle(CommitAccountDebit message)
        {
            throw new NotImplementedException();
        }

        public Task Handle(CreateAccount message)
        {
           var account = new BankAccount(message.AccountId,
                                         message.Name,
                                         message.CardId,
                                         message.Amount);
           _repository.Add(account);
           return Task.CompletedTask;
        }

        public Task Handle(PrepareAccountCredit message)
        {
            throw new NotImplementedException();
        }

        public Task Handle(PrepareAccountDebit message)
        {
            throw new NotImplementedException();
        }
    }
}