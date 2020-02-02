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
        public string FromAccountId { get; protected set; }
        public string ToAccountId { get; protected set; }
        public decimal Amount { get; protected set; }
    }

    public class SubmitTransaction : TransactionCommand
    {

    }

    public class PrepareTransactionDebit : TransactionCommand
    {

    }

    public class PrepareTransactionCredit : TransactionCommand
    {

    }

    public class CommitTransactionDebit : TransactionCommand
    {

    }

    public class CommitTransactionCredit : TransactionCommand
    {

    }

    public class FailTransactionPreparation : TransactionCommand
    {
        public string ReasonRemark { get; set; }
    }
}
