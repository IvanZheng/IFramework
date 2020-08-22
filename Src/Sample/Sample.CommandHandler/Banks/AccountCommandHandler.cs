using System;
using System.Threading;
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
    public class AccountCommandHandler : BankCommandHandler<BankAccount>,
                                         ICommandAsyncHandler<CreateAccount>,
                                         ICommandAsyncHandler<PrepareAccountCredit>,
                                         ICommandAsyncHandler<PrepareAccountDebit>,
                                         ICommandAsyncHandler<CommitAccountCredit>,
                                         ICommandAsyncHandler<CommitAccountDebit>,
                                         ICommandAsyncHandler<RevertAccountDebitPreparation>,
                                         ICommandAsyncHandler<RevertAccountCreditPreparation>
    {
        public AccountCommandHandler(IMessageContext commandContext, 
                                     IEventSourcingRepository<BankAccount> repository)
            :base(commandContext, repository)
        {
        }

        public async Task Handle(CommitAccountCredit message, CancellationToken cancellationToken)
        {
            var account = await GetAggregateRootAsync(message.AccountId).ConfigureAwait(false);
            account.CommitCredit(message.TransactionInfo);
        }

        public async Task Handle(CommitAccountDebit message, CancellationToken cancellationToken)
        {
            var account = await GetAggregateRootAsync(message.AccountId).ConfigureAwait(false);
            account.CommitDebit(message.TransactionInfo);
        }

        public async Task Handle(CreateAccount message, CancellationToken cancellationToken)
        {
            var account = await GetAggregateRootAsync(message.AccountId, 
                                                false).ConfigureAwait(false);

            if (account != null)
            {
                throw new DomainException(ErrorCode.BankAccountAlreadyExists, new object[] { message.AccountId });
            }
            account = new BankAccount(message.AccountId,
                                          message.Name,
                                          message.CardId,
                                          message.Amount);
            Repository.Add(account);
        }

        public async Task Handle(PrepareAccountCredit message, CancellationToken cancellationToken)
        {
            var account = await GetAggregateRootAsync(message.AccountId).ConfigureAwait(false);
            account.PrepareCredit(message.TransactionInfo);
        }

        public async Task Handle(PrepareAccountDebit message, CancellationToken cancellationToken)
        {
            var account = await GetAggregateRootAsync(message.AccountId).ConfigureAwait(false);
            account.PrepareDebit(message.TransactionInfo);
        }

        public async Task Handle(RevertAccountDebitPreparation message, CancellationToken cancellationToken)
        {
            var account = await GetAggregateRootAsync(message.AccountId).ConfigureAwait(false);
            account.RevertDebitPreparation(message.TransactionInfo);
        }

        public async Task Handle(RevertAccountCreditPreparation message, CancellationToken cancellationToken)
        {
            var account = await GetAggregateRootAsync(message.AccountId).ConfigureAwait(false);
            account.RevertCreditPreparation(message.TransactionInfo);
        }
    }
}