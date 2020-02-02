using System;
using System.Collections.Generic;
using System.Text;

namespace Sample.Command
{
    /// <summary>
    /// 账户流水
    /// </summary>
    public class CreateAccountStatement: CommandBase
    {
        public string AccountId { get; set; }
        public decimal Amount { get;set; }
        public DateTime Time { get;set; }
        public AccountStatementType Type { get; set; }
        /// <summary>
        /// 根据Type 判断ReferenceId类型，可能为TransactionId（用户转账相关操作）或 Account自身标识(用于充值转账）
        /// </summary>
        public string ReferenceId { get; set; }
        public string Description { get;set; }
    }
}
