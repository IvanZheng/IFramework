using System;
using System.Collections.Generic;
using System.Text;

namespace Sample.Command.Banks
{
    public class PrepareCredit: SerialCommandBase
    {
        public string AccountId { get; set; }
        public override string Key { get => AccountId; set => AccountId = value; }
    }
}
