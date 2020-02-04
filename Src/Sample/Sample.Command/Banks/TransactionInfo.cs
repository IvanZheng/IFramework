using System;
using IFramework.Domain;

namespace Sample.Command
{
    public class TransactionInfo : ValueObject<TransactionInfo>
    {
        public TransactionInfo() { }

        public TransactionInfo(string transactionId, string fromAccountId, string accountId, decimal amount, DateTime time)
        {
            if (FromAccountId == ToAccountId)
            {
                throw new Exception("From Account and To Account can't be the same.");
            }
            TransactionId = transactionId;
            FromAccountId = fromAccountId;
            ToAccountId = accountId;
            Amount = amount;
            Time = time;
        }
        public string TransactionId { get; protected set; }
        public string FromAccountId { get; protected set; }
        public string ToAccountId { get; protected set; }
        public decimal Amount { get; protected set; }
        public DateTime Time { get; protected set; }
    }
}