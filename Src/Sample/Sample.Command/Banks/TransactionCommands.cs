using System;
using System.Collections.Generic;
using System.Text;
using IFramework.Command.Impl;
using IFramework.Message;

namespace Sample.Command
{
    [Topic("BankCommandQueue")]
    public class TransactionCommand: SerialCommandBase
    {
        [SerialKey]
        public string TransactionId { get; set; }
        public TransactionInfo TransactionInfo { get; set; }

        public TransactionCommand(string transactionId, TransactionInfo transactionInfo)
        {
            TransactionId = transactionId;
            TransactionInfo = transactionInfo;
        }
    }

    [Topic("BankCommandQueue")]
    public class SubmitTransaction : SerialCommandBase
    {
        [SerialKey]
        public string TransactionId { get; set; }
        public string DebitAccountId { get; set; }
        public string CreditAccountId { get; set; }
        public decimal Amount { get; set; }
    }

    public class PrepareTransactionDebit : TransactionCommand
    {
        public PrepareTransactionDebit(string transactionId, TransactionInfo transactionInfo) : base(transactionId, transactionInfo) { }
    }

    public class PrepareTransactionCredit : TransactionCommand
    {
        public PrepareTransactionCredit(string transactionId, TransactionInfo transactionInfo) : base(transactionId, transactionInfo) { }
    }

    public class CommitTransactionDebit : TransactionCommand
    {
        public CommitTransactionDebit(string transactionId, TransactionInfo transactionInfo) : base(transactionId, transactionInfo) { }
    }

    public class CommitTransactionCredit : TransactionCommand
    {
        public CommitTransactionCredit(string transactionId, TransactionInfo transactionInfo) : base(transactionId, transactionInfo) { }
    }

    public class FailTransactionPreparation : TransactionCommand
    {
        public string FailReason { get; set; }
        public FailTransactionPreparation(string transactionId, TransactionInfo transactionInfo, string failReason)
            : base(transactionId, transactionInfo)
        {
            FailReason = failReason;
        }
    }
}
