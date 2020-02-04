using System;
using IFramework.Domain;

namespace Sample.Command
{
    public class TransactionInfo : ValueObject<TransactionInfo>
    {
        public TransactionInfo() { }

        public TransactionInfo(string transactionId, string debitAccountId, string creditAccountId, decimal amount, DateTime time)
        {
            if (DebitAccountId == CreditAccountId)
            {
                throw new Exception("From Account and To Account can't be the same.");
            }

            TransactionId = transactionId;
            DebitAccountId = debitAccountId;
            CreditAccountId = creditAccountId;
            Amount = amount;
            Time = time;
        }

        public string TransactionId { get; protected set; }

        /// <summary>
        ///     出账帐号标识
        /// </summary>
        public string DebitAccountId { get; protected set; }

        /// <summary>
        ///     入账帐号标识
        /// </summary>
        public string CreditAccountId { get; protected set; }

        public DateTime Time { get; protected set; }

        /// <summary>
        ///     转账金额
        /// </summary>
        public decimal Amount { get; protected set; }
    }
}