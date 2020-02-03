using System;
using System.Collections.Generic;
using System.Text;
using IFramework.Infrastructure.EventSourcing.Domain;
using Sample.Command;

namespace Sample.Domain.Model.BankAccounts
{
    public class Transaction: EventSourcingAggregateRoot
    {
        /// <summary>
        /// 出账帐号标识
        /// </summary>
        public string DebitAccountId { get; private set; }
        /// <summary>
        /// 入账帐号标识
        /// </summary>
        public string CreditAccountId { get; private set; }
        /// <summary>
        /// 转账金额
        /// </summary>
        public decimal Amount { get; private set; }
        /// <summary>
        /// 是否入账准备成功
        /// </summary>
        public bool? CreditPrepared { get; private set; }
        /// <summary>
        /// 是否出账准备成功
        /// </summary>
        public bool? DebitPrepared { get; private set; }
        /// <summary>
        /// 入账完成
        /// </summary>
        public bool? CreditCommitted { get; private set; }
        /// <summary>
        /// 出账完成
        /// </summary>
        public bool? DebitCommitted { get; private set; }
        /// <summary>
        /// 转账交易状态
        /// </summary>
        public BankTransactionStatus Status { get; private set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; private set; } 
    }
}
