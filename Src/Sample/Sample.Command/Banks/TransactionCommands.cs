using System;
using System.Collections.Generic;
using System.Text;

namespace Sample.Command.Banks
{
    public class CreateTransaction: CommandBase
    {
        public string TransactionId { get; set; }
        public string FromAccountId { get; set; }
        public string ToAccountId { get; set; }
        public decimal Amount { get; set; }
    }
}
