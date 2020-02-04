using System;
using System.Threading.Tasks;
using IFramework.Command;
using IFramework.Exceptions;
using IFramework.Infrastructure.EventSourcing.Repositories;
using IFramework.Message;
using Sample.Command;
using Sample.Domain.Model.Bank.Accounts;
using ErrorCode = Sample.DTO.ErrorCode;

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

        protected async Task<BankAccount> GetAccountAsync(string accountId, bool throwExceptionIfNotExists = true)
        {
            var account = await _repository.GetByKeyAsync(accountId)
                                           .ConfigureAwait(false);
            if (account == null && throwExceptionIfNotExists)
            {
                throw new DomainException(ErrorCode.ObjectNotExists, new object[] {accountId});
            }

            return account;
        }


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

        public async Task Handle(CreateAccount message)
        {
            var account = await GetAccountAsync(message.AccountId, 
                                                false).ConfigureAwait(false);

            if (account != null)
            {
                throw new DomainException(ErrorCode.BankAccountAlreadyExists, new object[] { message.AccountId });
            }
            account = new BankAccount(message.AccountId,
                                          message.Name,
                                          message.CardId,
                                          message.Amount);
            _repository.Add(account);
        }

        public async Task Handle(PrepareAccountCredit message)
        {
            var account = await GetAccountAsync(message.AccountId).ConfigureAwait(false);
            account.PrepareCredit(message.TransactionInfo);
        }

        public Task Handle(PrepareAccountDebit message)
        {
            throw new NotImplementedException();
        }
    }
}