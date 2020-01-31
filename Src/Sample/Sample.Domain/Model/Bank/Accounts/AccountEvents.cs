using System;
using System.Collections.Generic;
using System.Text;
using IFramework.Event;
using Sample.DomainEvents;

namespace Sample.Domain.Model.Bank.Accounts
{
    public class DebitPrepared: AggregateRootEvent
    {
        public string TransactionId { get; private set; }

        public DebitPrepared(string accountId, string transactionId) : base(accountId)
        {
            TransactionId = transactionId;
        }
    }
}
