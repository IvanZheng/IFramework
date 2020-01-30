using System;
using System.Collections.Generic;
using System.Text;
using IFramework.Infrastructure.EventSourcing.Domain;
using Sample.Command.Banks;

namespace Sample.Domain.Model.BankAccounts
{
    public class Transaction: EventSourcingAggregateRoot
    {
        /// <summary>
        /// 扣款帐号标识
        /// </summary>
        public string DebitAccountId { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        public string CreditAccountId { get; private set; }
        public decimal Amount { get; private set; }

        public bool CreditPrepared { get; private set; }
        public bool DebitPrepared { get; private set; }

        public bool CreditCommitted { get; private set; }
        public bool DebitCommitted { get; private set; }

        public BankTransactionStatus Status { get; private set; }
        public string Remark { get; private set; }
    }
}
