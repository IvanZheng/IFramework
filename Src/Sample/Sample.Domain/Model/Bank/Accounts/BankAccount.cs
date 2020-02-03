using IFramework.Infrastructure.EventSourcing.Domain;
using Sample.Command;
using Sample.DomainEvents.Banks;

namespace Sample.Domain.Model.Bank.Accounts
{
    public class BankAccount : EventSourcingAggregateRoot
    {
        public BankAccount() { }

        public BankAccount(string id, string name, string cardId, decimal amount)
        {
            Initialize(id, name, cardId, amount);
        }

        public string Name { get; private set; }
        public string CardId { get; private set; }

        /// <summary>
        ///     可用余额
        /// </summary>
        public decimal AvailableFund { get; private set; }

        /// <summary>
        ///     总金额
        /// </summary>
        public decimal TotalFund { get; private set; }

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
            AvailableFund = accountCreated.Amount;
            TotalFund = accountCreated.Amount;
            Status = AccountStatus.Normal;
        }

        public void PrepareCredit(TransactionInfo transaction)
        {
            if (Status == AccountStatus.Normal)
            {
                OnEvent(new AccountCreditPrepared(Id, transaction));
            }
            else
            {
                OnException(new AccountCreditPrepareFailed(Id, transaction));
            }
        }

        public void CommitDebit(TransactionInfo transaction)
        {
            OnEvent(new AccountDebitCommitted(Id, transaction, TotalFund - transaction.Amount));
        }


        public void CommitCredit(TransactionInfo transaction)
        {
            OnEvent(new AccountCreditCommitted(Id, 
                                               transaction, 
                                               AvailableFund + transaction.Amount,
                                               TotalFund + transaction.Amount));
        }
        private void Handle(AccountCreditCommitted @event)
        {
            AvailableFund = @event.AvailableFund;
            TotalFund = @event.TotalFund;
        }

        public void PrepareDebit(TransactionInfo transaction)
        {
            if (Status != AccountStatus.Normal)
            {
                OnException(new AccountDebitPrepareFailed(Id, transaction, "Debit account status is not available."));
            }
            else if (AvailableFund < transaction.Amount)
            {
                OnException(new AccountDebitPrepareFailed(Id, transaction, "Debit account is without enough funds."));
            }
            else
            {
                OnEvent(new AccountDebitPrepared(Id, transaction, AvailableFund - transaction.Amount));
            }
        }

        private void Handle(AccountDebitPrepared @event)
        {
            AvailableFund = @event.AvailableFund;
        }

        public void RevertDebitPreparation(TransactionInfo transaction)
        {
            OnEvent(new DebitPreparationReverted(Id, transaction, AvailableFund + transaction.Amount));
        }

        private void Handle(DebitPreparationReverted @event)
        {
            AvailableFund = @event.AvailableFund;
        }
    }
}