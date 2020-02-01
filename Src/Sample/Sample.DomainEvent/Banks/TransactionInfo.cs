using System;
using IFramework.Domain;

namespace Sample.DomainEvents.Banks
{
    public class TransactionInfo : ValueObject<TransactionInfo>
    {
        public TransactionInfo() { }

        public TransactionInfo(string fromAccountId, string accountId, decimal amount, DateTime time)
        {
            FromAccountId = fromAccountId;
            ToAccountId = accountId;
            Amount = amount;
            Time = time;
        }

        public string FromAccountId { get; protected set; }
        public string ToAccountId { get; protected set; }
        public decimal Amount { get; protected set; }
        public DateTime Time { get; protected set; }
    }
}