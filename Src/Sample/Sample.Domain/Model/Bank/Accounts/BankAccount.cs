using IFramework.Infrastructure.EventSourcing.Domain;
using Sample.Command.Banks;
using Sample.DomainEvents.Banks;

namespace Sample.Domain.Model.BankAccounts
{
    public class BankAccount : EventSourcingAggregateRoot
    {
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

        private void Handle(AccountCreated accountCreated)
        {
            Id = accountCreated.Id;
            Name = accountCreated.Name;
            CardId = accountCreated.CardId;
            AvailableFund = accountCreated.Amount;
            TotalFund = accountCreated.Amount;
            Status = AccountStatus.Normal;
        }

        public BankAccount(string id, string name, string cardId, decimal amount)
        {
            Initialize(id, name, cardId, amount);
        }

        protected void Initialize(string id, string name, string cardId, decimal amount)
        {
            OnEvent(new AccountCreated(id, amount, name, cardId));
        }

        public void PrepareCredit(decimal amount, string transactionId) { }

        public void CommitCredit(decimal amount, string transactionId) { }

        public void PrepareDebit(decimal amount, string transactionId) { }

        public void RevertDebitPreparation(decimal amount, string transactionId) { }
    }
}