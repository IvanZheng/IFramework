using IFramework.Event;
using Sample.Command;
using BankErrorCode = Sample.DTO.ErrorCode;
namespace Sample.DomainEvents.Banks
{
    public abstract class AccountTransactionEvent : AggregateRootEvent
    {
        protected AccountTransactionEvent(object aggregateRootId, TransactionInfo transaction)
            : base(aggregateRootId)
        {
            Transaction = transaction;
        }

        public TransactionInfo Transaction { get; protected set; }
    }

    public abstract class AccountTransactionException : AccountTransactionEvent, IAggregateRootExceptionEvent
    {
        protected AccountTransactionException(object aggregateRootId, TransactionInfo transaction)
            : base(aggregateRootId, transaction) { }

        public abstract object ErrorCode { get; set; }
    }

    public class AccountDebitPrepared : AccountTransactionEvent
    {
        public AccountDebitPrepared(object aggregateRootId, TransactionInfo transaction, decimal availableBalance)
            : base(aggregateRootId, transaction)
        {
            AvailableBalance = availableBalance;
        }

        public decimal AvailableBalance { get; protected set; }
    }

    public class AccountCreditPrepared : AccountTransactionEvent
    {
        public AccountCreditPrepared(object aggregateRootId, TransactionInfo transaction)
            : base(aggregateRootId, transaction) { }
    }

    public class AccountDebitCommitted : AccountTransactionEvent
    {
        public decimal CurrentBalance { get; protected set; }

        public AccountDebitCommitted(object aggregateRootId, TransactionInfo transaction, decimal currentBalance)
            : base(aggregateRootId, transaction)
        {
            CurrentBalance = currentBalance;
        }
    }

    public class AccountCreditCommitted : AccountTransactionEvent
    {
        public decimal AvailableBalance { get; protected set; }
        public decimal CurrentBalance { get; protected set; }

        public AccountCreditCommitted(object aggregateRootId, TransactionInfo transaction, decimal availableBalance, decimal currentBalance)
            : base(aggregateRootId, transaction)
        {
            AvailableBalance = availableBalance;
            CurrentBalance = currentBalance;
        }
    }

    public class DebitPreparationReverted : AccountTransactionEvent
    {
        public decimal AvailableBalance { get; protected set; }
        public DebitPreparationReverted(object aggregateRootId, TransactionInfo transaction, decimal availableBalance) 
            : base(aggregateRootId, transaction)
        {
            AvailableBalance = availableBalance;
        }
    }

    public class AccountDebitPrepareFailed : AccountTransactionException
    {
        public AccountDebitPrepareFailed(object aggregateRootId, TransactionInfo transaction, string reason)
            : base(aggregateRootId, transaction)
        {
            Reason = reason;
        }

        public string Reason { get; protected set; }
        public override object ErrorCode { get; set; } = BankErrorCode.AccountDebitPrepareFailed;
    }

    public class AccountCreditPrepareFailed : AccountTransactionException
    {
        public AccountCreditPrepareFailed(object aggregateRootId, TransactionInfo transaction)
            : base(aggregateRootId, transaction) { }

        public override object ErrorCode { get; set; } = BankErrorCode.AccountCreditPrepareFailed;
    }

    public class AccountCreated : AggregateRootEvent
    {
        public AccountCreated(object aggregateRootId, decimal amount, string name, string cardId)
            : base(aggregateRootId)
        {
            Amount = amount;
            Name = name;
            CardId = cardId;
        }

        public decimal Amount { get; protected set; }
        public string Name { get; protected set; }
        public string CardId { get; protected set; }
    }

    public class AccountWithdrawn : AggregateRootEvent
    {
        public AccountWithdrawn(object aggregateRootId, decimal amount) : base(aggregateRootId)
        {
            Amount = amount;
        }

        public decimal Amount { get; protected set; }
    }

    public class AccountDeposited : AggregateRootEvent
    {
        public AccountDeposited(object aggregateRootId, decimal amount) : base(aggregateRootId)
        {
            Amount = amount;
        }

        public decimal Amount { get; protected set; }
    }
}