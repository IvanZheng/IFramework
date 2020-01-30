using System;
using System.Collections.Generic;
using System.Text;
using IFramework.Event;
using IFramework.Infrastructure.EventSourcing.Domain;
using Sample.Command.Banks;

namespace Sample.Domain.Model.BankAccounts
{
    public class BankAccount: EventSourcingAggregateRoot
    {
        public string Name { get; private set; }
        public string CardId { get; private set; }
        public decimal AvailableAmount { get; private set; }
        public decimal TotalAmount { get; private set; }
        public AccountStatus Status { get; private set; }
        public BankAccount(string id, string name, string cardId, decimal amount)
        {
            Id = id;
            Name = name;
            CardId = cardId;
            CardId = cardId;
            AvailableAmount = amount;
            TotalAmount = amount;
            Status = AccountStatus.Normal;
        }

        public void Credit(decimal amount, object referenceType, string referenceId = null)
        {

        }
    }
}
