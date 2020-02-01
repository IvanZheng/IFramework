namespace Sample.DomainEvents.Banks
{
    public abstract class AccountTransactionEvent : AggregateRootEvent
    {
        protected AccountTransactionEvent(object aggregateRootId, decimal amount, string transactionId)
            : base(aggregateRootId)
        {
            Amount = amount;
            TransactionId = transactionId;
        }

        public decimal Amount { get; protected set; }
        public string TransactionId { get; protected set; }
    }

    public class AccountDebitPrepared : AccountTransactionEvent
    {
        public AccountDebitPrepared(object aggregateRootId, decimal amount, string transactionId)
            : base(aggregateRootId, amount, transactionId) { }
    }

    public class AccountCreditPrepared : AccountTransactionEvent
    {
        public AccountCreditPrepared(object aggregateRootId, decimal amount, string transactionId)
            : base(aggregateRootId, amount, transactionId) { }
    }

    public class AccountDebitCommitted : AccountTransactionEvent
    {
        public AccountDebitCommitted(object aggregateRootId, decimal amount, string transactionId)
            : base(aggregateRootId, amount, transactionId) { }
    }

    public class AccountCreditCommitted : AccountTransactionEvent
    {
        public AccountCreditCommitted(object aggregateRootId, decimal amount, string transactionId)
            : base(aggregateRootId, amount, transactionId) { }
    }

    public class AccountDebitPrepareFailed : AccountTransactionEvent
    {
        public AccountDebitPrepareFailed(object aggregateRootId, decimal amount, string transactionId)
            : base(aggregateRootId, amount, transactionId) { }
    }

    public class AccountCreditPrepareFailed : AccountTransactionEvent
    {
        public AccountCreditPrepareFailed(object aggregateRootId, decimal amount, string transactionId)
            : base(aggregateRootId, amount, transactionId) { }
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