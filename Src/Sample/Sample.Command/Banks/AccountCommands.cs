namespace Sample.Command.Banks
{
    public class CreateAccount : SerialCommandBase
    {
        public string AccountId { get; set; }
        public string Name { get; set; }
        public string CardId { get; set; }
        public decimal Amount { get; set; }

        public override string Key
        {
            get => AccountId;
            set => AccountId = value;
        }
    }

    public class CommitCredit : SerialCommandBase
    {
        public string AccountId { get; set; }
        public decimal Amount { get; set; }
        public string TransactionId { get; set; }

        public override string Key
        {
            get => AccountId;
            set => AccountId = value;
        }
    }

    public class CommitDebit : SerialCommandBase
    {
        public string AccountId { get; set; }
        public decimal Amount { get; set; }
        public string TransactionId { get; set; }

        public override string Key
        {
            get => AccountId;
            set => AccountId = value;
        }
    }

    public class PrepareCredit : SerialCommandBase
    {
        public string AccountId { get; set; }

        public override string Key
        {
            get => AccountId;
            set => AccountId = value;
        }
    }

    public class PrepareDebit : SerialCommandBase
    {
        public string AccountId { get; set; }

        public override string Key
        {
            get => AccountId;
            set => AccountId = value;
        }
    }

    public class RevertDebitPreparation : SerialCommandBase
    {
        public string AccountId { get; set; }
        public decimal Amount { get; set; }
        public decimal TransactionId { get; set; }

        public override string Key
        {
            get => AccountId;
            set => AccountId = value;
        }
    }
}