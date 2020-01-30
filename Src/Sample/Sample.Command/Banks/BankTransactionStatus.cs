using System;
using System.Collections.Generic;
using System.Text;

namespace Sample.Command.Banks
{
    public enum BankTransactionStatus
    {
        Initialized,
        Prepared,
        PrepareFailed,
        Completed
    }
}
