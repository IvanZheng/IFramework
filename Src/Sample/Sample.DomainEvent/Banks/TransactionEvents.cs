using Sample.Command;

namespace Sample.DomainEvents.Banks
{
    public abstract class TransactionEvent : AggregateRootEvent
    {
        protected TransactionEvent(object aggregateRootId, TransactionInfo transaction) : base(aggregateRootId)
        {
            Transaction = transaction;
        }

        public TransactionInfo Transaction { get; protected set; }
    }

    public class TransactionSubmitted : TransactionEvent
    {
        public TransactionSubmitted(object aggregateRootId, TransactionInfo transaction)
            : base(aggregateRootId, transaction) { }
    }

    public class TransactionFailed : TransactionEvent
    {
        public string FailReason { get; protected set; }

        public TransactionFailed(object aggregateRootId, TransactionInfo transaction, string failReason)
            : base(aggregateRootId, transaction)
        {
            FailReason = failReason;
        }
    }

    public class TransactionDebitPrepared : TransactionEvent
    {
        public TransactionDebitPrepared(object aggregateRootId, TransactionInfo transaction)
            : base(aggregateRootId, transaction) { }
    }

    public class TransactionCreditPrepared : TransactionEvent
    {
        public TransactionCreditPrepared(object aggregateRootId, TransactionInfo transaction)
            : base(aggregateRootId, transaction) { }
    }

    public class TransactionPrepared : TransactionEvent
    {
        public TransactionPrepared(object aggregateRootId, TransactionInfo transaction)
            : base(aggregateRootId, transaction) { }
    }

    public class TransactionDebitCommitted : TransactionEvent
    {
        public TransactionDebitCommitted(object aggregateRootId, TransactionInfo transaction)
            : base(aggregateRootId, transaction) { }
    }

    public class TransactionCreditCommitted : TransactionEvent
    {
        public TransactionCreditCommitted(object aggregateRootId, TransactionInfo transaction)
            : base(aggregateRootId, transaction) { }
    }

    public class TransactionCompleted : TransactionEvent
    {
        public TransactionCompleted(object aggregateRootId, TransactionInfo transaction)
            : base(aggregateRootId, transaction) { }
    }
}