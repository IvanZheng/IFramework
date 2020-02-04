using System;
using System.Threading.Tasks;
using IFramework.Command;
using IFramework.Event;
using IFramework.Infrastructure.EventSourcing.Repositories;
using IFramework.Message;
using Sample.Command;
using Sample.Command.Banks;
using Sample.Domain.Model.Bank.Transactions;

namespace Sample.CommandHandler.Banks
{
    public class TransactionCommandHandler : BankCommandHandler<Transaction>,
                                             ICommandAsyncHandler<SubmitTransaction>,
                                             ICommandAsyncHandler<PrepareTransactionCredit>,
                                             ICommandAsyncHandler<PrepareTransactionDebit>,
                                             ICommandAsyncHandler<CommitTransactionDebit>,
                                             ICommandAsyncHandler<CommitTransactionCredit>,
                                             ICommandAsyncHandler<FailTransactionPreparation>
    {
        private readonly IEventBus _eventBus;

        public TransactionCommandHandler(IMessageContext commandContext,
                                         IEventSourcingRepository<Transaction> repository,
                                         IEventBus eventBus) :
            base(commandContext, repository)
        {
            _eventBus = eventBus;
        }

        public async Task Handle(CommitTransactionCredit message)
        {
            var transaction = await GetAggregateRootAsync(message.TransactionId).ConfigureAwait(false);
            transaction.CommitCredit();
            if (transaction.Status == BankTransactionStatus.Completed)
            {
                _eventBus.FinishSaga(new TransactionResult());
            }
        }

        public async Task Handle(CommitTransactionDebit message)
        {
            var transaction = await GetAggregateRootAsync(message.TransactionId).ConfigureAwait(false);
            transaction.CommitDebit();
            if (transaction.Status == BankTransactionStatus.Completed)
            {
                _eventBus.FinishSaga(new TransactionResult());
            }
        }

        public async Task Handle(FailTransactionPreparation message)
        {
            var transaction = await GetAggregateRootAsync(message.TransactionId).ConfigureAwait(false);
            transaction.FailPrepare(message.FailReason);
            _eventBus.FinishSaga(new TransactionResult(false, message.FailReason));
        }

        public async Task Handle(PrepareTransactionCredit message)
        {
            var transaction = await GetAggregateRootAsync(message.TransactionId).ConfigureAwait(false);
            transaction.PrepareCredit();
        }

        public async Task Handle(PrepareTransactionDebit message)
        {
            var transaction = await GetAggregateRootAsync(message.TransactionId).ConfigureAwait(false);
            transaction.PrepareDebit();
        }

        public Task Handle(SubmitTransaction message)
        {
            var transaction = new Transaction(message.TransactionId,
                                              new TransactionInfo(message.TransactionId,
                                                                  message.DebitAccountId,
                                                                  message.CreditAccountId,
                                                                  message.Amount,
                                                                  DateTime.Now));
            Repository.Add(transaction);
            return Task.CompletedTask;
        }
    }
}