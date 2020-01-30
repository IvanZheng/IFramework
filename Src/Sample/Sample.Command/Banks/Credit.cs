using System;
using System.Collections.Generic;
using System.Text;

namespace Sample.Command.Banks
{
    public class Credit: SerialCommandBase
    {
        public string AccountId { get; set; }
        public decimal Amount { get; set; }
        public string TransactionId { get; set; }
        public override string Key { get => AccountId; set => AccountId = value; }
    }
}
