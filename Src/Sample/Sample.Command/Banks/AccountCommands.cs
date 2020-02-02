using IFramework.Command.Impl;
using IFramework.Message;

namespace Sample.Command
{
    [Topic("BankCommandQueue")]
    public abstract class AccountCommand : SerialCommandBase
    {
        [SerialKey]
        public string AccountId { get; set; }
    }

    public abstract class AccountTransactionCommand : AccountCommand
    {
        public TransactionInfo TransactionInfo { get; set; }
    }

    public class CreateAccount : AccountCommand
    {
        public string Name { get; set; }
        public string CardId { get; set; }
        public decimal Amount { get; set; }
    }

    public class CommitAccountCredit : AccountTransactionCommand
    {
        
    }

    public class CommitAccountDebit : AccountTransactionCommand
    {
     
    }

    public class PrepareAccountCredit : AccountTransactionCommand
    {
    }

    public class PrepareAccountDebit : AccountTransactionCommand
    {
    }

    public class RevertAccountDebitPreparation : AccountTransactionCommand
    {
    }
}