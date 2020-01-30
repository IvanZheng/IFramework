using System;
using System.Collections.Generic;
using System.Text;

namespace Sample.Command.Banks
{
    public class PrepareDebit: SerialCommandBase
    {
        public string AccountId { get; set; }
        public override string Key { get => AccountId; set => AccountId = value; }
    }
}
