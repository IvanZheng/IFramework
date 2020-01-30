using System;
using System.Collections.Generic;
using System.Text;

namespace Sample.Command.Banks
{
    public class CreateBankAccount: SerialCommandBase
    {
        public string AccountId { get; set; }
        public string Name { get; set; }
        public string CardId { get; set; }
        public decimal Amount { get; set; }

        public override string Key { get => AccountId; set => AccountId = value; }
    }
}
