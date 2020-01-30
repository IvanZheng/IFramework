namespace Sample.Command.Banks
{
    public class Debit : SerialCommandBase
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
}