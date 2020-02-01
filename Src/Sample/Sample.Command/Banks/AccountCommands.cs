namespace Sample.Command.Banks
{
    public abstract class AccountCommand : SerialCommandBase
    {
        public string AccountId { get; set; }
        public override string Key
        {
            get => AccountId;
            set => AccountId = value;
        }
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