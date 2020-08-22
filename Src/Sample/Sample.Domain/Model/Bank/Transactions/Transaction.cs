using System;
using IFramework.Infrastructure.EventSourcing.Domain;
using Sample.Command;
using Sample.DomainEvents.Banks;

namespace Sample.Domain.Model.Bank.Transactions
{
    public class Transaction : EventSourcingAggregateRoot
    {
        public Transaction() { }

        public Transaction(string id, TransactionInfo transactionInfo)
        {
            Initialize(id, transactionInfo);
        }

        public TransactionInfo Info { get; protected set; }

        /// <summary>
        ///     是否入账准备成功
        /// </summary>
        public bool CreditPrepared { get; protected set; }

        /// <summary>
        ///     是否出账准备成功
        /// </summary>
        public bool DebitPrepared { get; protected set; }

        /// <summary>
        ///     入账完成
        /// </summary>
        public bool CreditCommitted { get; protected set; }

        /// <summary>
        ///     出账完成
        /// </summary>
        public bool DebitCommitted { get; protected set; }

        /// <summary>
        ///     转账交易状态
        /// </summary>
        public BankTransactionStatus Status { get; protected set; }

        /// <summary>
        ///     备注
        /// </summary>
        public string Remark { get; protected set; }

        private void Initialize(string id, TransactionInfo transactionInfo)
        {
            OnEvent(new TransactionSubmitted(id, transactionInfo));
        }

        private void Handle(TransactionSubmitted @event)
        {
            Id = @event.AggregateRootId.ToString();
            Info = @event.Transaction;
            Status = BankTransactionStatus.Initialized;
        }

        public void PrepareDebit()
        {
            OnEvent(new TransactionDebitPrepared(Id, Info));
            TryPrepare();
        }

        private void Handle(TransactionDebitPrepared @event)
        {
            DebitPrepared = true;
        }

        public void PrepareCredit()
        {
            OnEvent(new TransactionCreditPrepared(Id, Info));
            TryPrepare();
        }

        private void Handle(TransactionCreditPrepared @event)
        {
            CreditPrepared = true;
        }

        public void FailPrepare(string failReason)
        {
            OnEvent(new TransactionFailed(Id, Info, failReason));
        }

        private void Handle(TransactionFailed @event)
        {
            Remark = @event.FailReason;
            Status = BankTransactionStatus.PrepareFailed;
        }

        private void TryPrepare()
        {
            if (DebitPrepared && CreditPrepared)
            {
                OnEvent(new TransactionPrepared(Id, Info));
            }
        }

        private void Handle(TransactionPrepared @event)
        {
            Status = BankTransactionStatus.Prepared;
        }

        public void CommitDebit()
        {
            OnEvent(new TransactionDebitCommitted(Id, Info));
            TryComplete();
        }

        private void Handle(TransactionDebitCommitted @event)
        {
            DebitCommitted = true;
        }

        public void CommitCredit()
        {
            OnEvent(new TransactionCreditCommitted(Id, Info));
            TryComplete();
        }

        private void Handle(TransactionCreditCommitted @event)
        {
            CreditCommitted = true;
        }

        private void TryComplete()
        {
            if (DebitCommitted && CreditCommitted)
            {
                OnEvent(new TransactionCompleted(Id, Info));
            }
        }

        private void Handle(TransactionCompleted @event)
        {
            Status = BankTransactionStatus.Completed;
        }
    }
}