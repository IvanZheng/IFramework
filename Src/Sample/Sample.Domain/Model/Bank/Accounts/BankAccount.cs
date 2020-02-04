using System;
using System.Collections.Generic;
using IFramework.Infrastructure.EventSourcing.Domain;
using Sample.Command;
using Sample.DomainEvents.Banks;
using IFramework.Infrastructure;

namespace Sample.Domain.Model.Bank.Accounts
{
    public class BankAccount : EventSourcingAggregateRoot
    {
        public BankAccount() { }

        public BankAccount(string id, string name, string cardId, decimal amount)
        {
            Initialize(id, name, cardId, amount);
        }

        public Dictionary<string, TransactionStatement> TransactionStatements { get; protected set; } = new Dictionary<string, TransactionStatement>();

        public string Name { get; private set; }
        public string CardId { get; private set; }

        /// <summary>
        ///     可用余额
        /// </summary>
        public decimal AvailableBalance { get; private set; }

        /// <summary>
        ///     当前余额
        /// </summary>
        public decimal CurrentBalance { get; private set; }

        /// <summary>
        ///     账户状态
        /// </summary>
        public AccountStatus Status { get; private set; }


        private void Initialize(string id, string name, string cardId, decimal amount)
        {
            OnEvent(new AccountCreated(id, amount, name, cardId));
        }

        private void Handle(AccountCreated accountCreated)
        {
            Id = accountCreated.Id;
            Name = accountCreated.Name;
            CardId = accountCreated.CardId;
            AvailableBalance = accountCreated.Amount;
            CurrentBalance = accountCreated.Amount;
            Status = AccountStatus.Normal;
        }

        public void PrepareCredit(TransactionInfo transaction)
        {
            if (TransactionStatements.ContainsKey(transaction.TransactionId))
            {
                return;
            }

            if (transaction.ToAccountId != Id)
            {
                throw new Exception("Transaction's credit accountId is not the same as target's id.");
            }

            if (Status == AccountStatus.Normal)
            {
                OnEvent(new AccountCreditPrepared(Id, transaction));
            }
            else
            {
                OnException(new AccountCreditPrepareFailed(Id, transaction));
            }
        }

        private void Handle(AccountCreditPrepared @event)
        {
            TransactionStatements.Add(@event.Transaction.TransactionId,
                                      new TransactionStatement(@event.Transaction,
                                                               TransactionType.Credit));
        }

        public void PrepareDebit(TransactionInfo transaction)
        {
            if (TransactionStatements.ContainsKey(transaction.TransactionId))
            {
                return;
            }

            if (transaction.FromAccountId != Id)
            {
                throw new Exception("Transaction's debit accountId is not the same as target's id.");
            }
            if (Status != AccountStatus.Normal)
            {
                OnException(new AccountDebitPrepareFailed(Id, transaction, "Debit account status is not available."));
            }
            else if (AvailableBalance < transaction.Amount)
            {
                OnException(new AccountDebitPrepareFailed(Id, transaction, "Debit account is without enough funds."));
            }
            else
            {
                OnEvent(new AccountDebitPrepared(Id, transaction, AvailableBalance - transaction.Amount));
            }
        }

        private void Handle(AccountDebitPrepared @event)
        {
            TransactionStatements.Add(@event.Transaction.TransactionId,
                                      new TransactionStatement(@event.Transaction,
                                                               TransactionType.Debit));
            AvailableBalance = @event.AvailableBalance;
        }


        public void CommitDebit(TransactionInfo transaction)
        {
            if (TransactionStatements.TryGetValue(transaction.TransactionId)?.Type != TransactionType.Debit)
            {
                throw new Exception("Invalid transaction debit command for the target account.");
            }
            OnEvent(new AccountDebitCommitted(Id, transaction, CurrentBalance - transaction.Amount));
        }

        private void Handle(AccountDebitCommitted @event)
        {
            TransactionStatements.TryRemove(@event.Transaction.TransactionId);
            CurrentBalance = @event.CurrentBalance;
        }


        public void CommitCredit(TransactionInfo transaction)
        {
            if (TransactionStatements.TryGetValue(transaction.TransactionId)?.Type != TransactionType.Credit)
            {
                throw new Exception("Invalid transaction credit command for the target account.");
            }
            OnEvent(new AccountCreditCommitted(Id,
                                               transaction,
                                               AvailableBalance + transaction.Amount,
                                               CurrentBalance + transaction.Amount));
        }

        private void Handle(AccountCreditCommitted @event)
        {
            TransactionStatements.TryRemove(@event.Transaction.TransactionId);
            AvailableBalance = @event.AvailableBalance;
            CurrentBalance = @event.CurrentBalance;
        }

        public void RevertDebitPreparation(TransactionInfo transaction)
        {
            if (TransactionStatements.TryGetValue(transaction.TransactionId)?.Type != TransactionType.Debit)
            {
                throw new Exception("Invalid transaction revert debit command for the target account.");
            }
            OnEvent(new DebitPreparationReverted(Id, transaction, AvailableBalance + transaction.Amount));
        }

        private void Handle(DebitPreparationReverted @event)
        {
            TransactionStatements.TryRemove(@event.Transaction.TransactionId);
            AvailableBalance = @event.AvailableBalance;
        }
    }
}