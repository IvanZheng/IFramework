using System;
using System.Collections.Generic;
using System.Text;

namespace Sample.Command
{
    public enum AccountStatus
    {
        Normal,
        Frozen,
        Deleted
    }

    public enum BankTransactionStatus
    {
        Initialized,
        Prepared,
        PrepareFailed,
        Completed
    }

    public enum AccountStatementType
    {
        /// <summary>
        /// 扣款转账准备
        /// </summary>
        PrepareDebit,
        /// <summary>
        /// 撤销扣款转账准备
        /// </summary>
        RevertDebit,
        /// <summary>
        /// 提交转账扣款
        /// </summary>
        CommitDebit,
        /// <summary>
        /// 提交转账入账
        /// </summary>
        CommitCredit,
        /// <summary>
        /// 提现
        /// </summary>
        Withdraw,
        /// <summary>
        /// 充值
        /// </summary>
        Deposit
    }
}
