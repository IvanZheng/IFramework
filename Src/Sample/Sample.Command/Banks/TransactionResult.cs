using System;
using System.Collections.Generic;
using System.Text;

namespace Sample.Command.Banks
{
    public class TransactionResult
    {
        public bool Success { get; set; }
        public string Remark { get; set; }

        public TransactionResult()
        {
            Success = true;
        }
        public TransactionResult(bool success, string remark = null)
        {
            Success = success;
            Remark = remark;
        }
    }
}
