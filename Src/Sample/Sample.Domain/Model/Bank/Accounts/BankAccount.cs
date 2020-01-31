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
        /// <summary>
        /// 可用余额
        /// </summary>
        public decimal AvailableAmount { get; private set; }
        /// <summary>
        /// 总金额
        /// </summary>
        public decimal TotalAmount { get; private set; }
        /// <summary>
        /// 账户状态
        /// </summary>
        public AccountStatus Status { get; private set; }

        public BankAccount(){}

        public BankAccount(string id, string name, string cardId, decimal amount)
        {
            Id = id;
            Name = name;
            CardId = cardId;
            AvailableAmount = amount;
            TotalAmount = amount;
            Status = AccountStatus.Normal;
        }

        public void PrepareCredit(decimal amount, string transactionId)
        {

        }

        public void CommitCredit(decimal amount, string transactionId)
        {

        }

        public void PrepareDebit(decimal amount, string transactionId)
        {

        }

        public void RevertDebitPreparation(decimal amount, string transactionId)
        {

        }
    }
}
