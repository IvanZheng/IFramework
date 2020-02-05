using IFramework.Command.Impl;
using IFramework.Message;

namespace Sample.Command
{
    [Topic("BankCommandQueue")]
    public abstract class AccountCommand : SerialCommandBase
    {
        [SerialKey]
        public string AccountId { get; set; }

        protected AccountCommand(string accountId)
        {
            AccountId = accountId;
        }
    }

    public abstract class AccountTransactionCommand : AccountCommand
    {
        public TransactionInfo TransactionInfo { get; set; }
        protected AccountTransactionCommand(string accountId, TransactionInfo transactionInfo)
            : base(accountId)
        {
            TransactionInfo = transactionInfo;
        }
    }

    public class CreateAccount : AccountCommand
    {
        public string Name { get; set; }
        public string CardId { get; set; }
        public decimal Amount { get; set; }
        public CreateAccount(string accountId) : base(accountId) { }
    }

    public class CommitAccountCredit : AccountTransactionCommand
    {
        public CommitAccountCredit(string accountId, TransactionInfo transactionInfo) : base(accountId, transactionInfo) { }
    }

    public class CommitAccountDebit : AccountTransactionCommand
    {
        public CommitAccountDebit(string accountId, TransactionInfo transactionInfo) : base(accountId, transactionInfo) { }
    }

    public class PrepareAccountCredit : AccountTransactionCommand
    {
        public PrepareAccountCredit(string accountId, TransactionInfo transactionInfo) : base(accountId, transactionInfo) { }
    }

    public class PrepareAccountDebit : AccountTransactionCommand
    {
        public PrepareAccountDebit(string accountId, TransactionInfo transactionInfo) : base(accountId, transactionInfo) { }
    }

    public class RevertAccountDebitPreparation : AccountTransactionCommand
    {
        public RevertAccountDebitPreparation(string accountId, TransactionInfo transactionInfo) : base(accountId, transactionInfo) { }
    }

    public class RevertAccountCreditPreparation : AccountTransactionCommand
    {
        public RevertAccountCreditPreparation(string accountId, TransactionInfo transactionInfo) : base(accountId, transactionInfo) { }
    }
}