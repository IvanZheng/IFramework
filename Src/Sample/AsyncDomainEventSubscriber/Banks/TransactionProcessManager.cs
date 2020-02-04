using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using IFramework.Event;
using IFramework.Message;
using Sample.Command;
using Sample.DomainEvents.Banks;

namespace Sample.AsyncDomainEventSubscriber.Banks
{
    public class TransactionProcessManager: IEventAsyncSubscriber<TransactionSubmitted>,
                                            IEventAsyncSubscriber<AccountDebitPrepared>,
                                            IEventAsyncSubscriber<AccountCreditPrepared>,
                                            IEventAsyncSubscriber<AccountDebitPrepareFailed>,
                                            IEventAsyncSubscriber<AccountCreditPrepareFailed>,
                                            IEventAsyncSubscriber<TransactionPrepared>,
                                            IEventAsyncSubscriber<AccountCreditCommitted>,
                                            IEventAsyncSubscriber<AccountDebitCommitted>
    {
        private readonly IEventBus _eventBus;
        private readonly IMessageContext _eventContext;
        public TransactionProcessManager(IEventBus eventBus, IMessageContext eventContext)
        {
            _eventBus = eventBus;
            _eventContext = eventContext;
        }

        public Task Handle(TransactionSubmitted message)
        {
            _eventBus.SendCommand(new PrepareAccountCredit(message.Transaction.CreditAccountId,
                                                           message.Transaction));
            _eventBus.SendCommand(new PrepareAccountDebit(message.Transaction.DebitAccountId,
                                                           message.Transaction));
            return Task.CompletedTask;
        }

        public Task Handle(AccountDebitPrepared message)
        {
            _eventBus.SendCommand(new PrepareTransactionDebit(message.Transaction.TransactionId,
                                                              message.Transaction));
            return Task.CompletedTask;
        }

        public Task Handle(AccountCreditPrepared message)
        {
            _eventBus.SendCommand(new PrepareTransactionCredit(message.Transaction.TransactionId,
                                                               message.Transaction));
            return Task.CompletedTask;
        }

        public Task Handle(AccountDebitPrepareFailed message)
        {
            _eventBus.SendCommand(new FailTransactionPreparation(message.Transaction.TransactionId,
                                                                 message.Transaction,
                                                                 message.Reason));
            _eventBus.SendCommand(new RevertAccountCreditPreparation(message.Transaction.CreditAccountId, 
                                                                     message.Transaction));
            return Task.CompletedTask;
        }

        public Task Handle(AccountCreditPrepareFailed message)
        {
            _eventBus.SendCommand(new FailTransactionPreparation(message.Transaction.TransactionId,
                                                                 message.Transaction,
                                                                 message.Reason));
            _eventBus.SendCommand(new RevertAccountDebitPreparation(message.Transaction.DebitAccountId, 
                                                                    message.Transaction));
            return Task.CompletedTask;
        }

        public Task Handle(TransactionPrepared message)
        {
            _eventBus.SendCommand(new CommitAccountCredit(message.Transaction.CreditAccountId,
                                                          message.Transaction));
            _eventBus.SendCommand(new CommitAccountDebit(message.Transaction.DebitAccountId,
                                                         message.Transaction));
            return Task.CompletedTask;
        }

        public Task Handle(AccountCreditCommitted message)
        {
            _eventBus.SendCommand(new CommitTransactionCredit(message.Transaction.TransactionId,
                                                              message.Transaction));
            return Task.CompletedTask;
        }

        public Task Handle(AccountDebitCommitted message)
        {
            _eventBus.SendCommand(new CommitTransactionDebit(message.Transaction.TransactionId,
                                                             message.Transaction));
            return Task.CompletedTask;
        }
    }
}
