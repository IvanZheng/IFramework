using System;
using System.Collections.Generic;
using System.Text;

namespace Sample.Command.Banks
{
    public class TransactionCommand: SerialCommandBase
    {
        public string TransactionId { get; set; }
        public string FromAccountId { get; protected set; }
        public string ToAccountId { get; protected set; }
        public decimal Amount { get; protected set; }

        public override string Key { get => TransactionId; set => TransactionId = value; }
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
