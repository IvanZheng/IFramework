using System;
using System.Collections.Generic;
using System.Text;

namespace Sample.Command.Banks
{
    public class CreateAccountStatement: CommandBase
    {
        public string AccountId { get; set; }
        public decimal Amount { get;set; }
        public DateTime Time { get;set; }
        public string Description { get;set; }
    }
}
