using Sample.Command;

namespace Sample.DomainEvents.Banks
{
    public abstract class TransactionEvent : AggregateRootEvent
    {
        protected TransactionEvent(object aggregateRootId, TransactionInfo info) : base(aggregateRootId)
        {
            Info = info;
        }

        public TransactionInfo Info { get; protected set; }
    }

    public class TransactionSubmitted : TransactionEvent
    {
        public TransactionSubmitted(object aggregateRootId, TransactionInfo info)
            : base(aggregateRootId, info) { }
    }

    public class TransactionFailed : TransactionEvent
    {
        public TransactionFailed(object aggregateRootId, TransactionInfo info)
            : base(aggregateRootId, info) { }
    }

    public class TransactionDebitPrepared : TransactionEvent
    {
        public TransactionDebitPrepared(object aggregateRootId, TransactionInfo info)
            : base(aggregateRootId, info) { }
    }

    public class TransactionCreditPrepared : TransactionEvent
    {
        public TransactionCreditPrepared(object aggregateRootId, TransactionInfo info)
            : base(aggregateRootId, info) { }
    }

    public class TransactionPrepared : TransactionEvent
    {
        public TransactionPrepared(object aggregateRootId, TransactionInfo info)
            : base(aggregateRootId, info) { }
    }

    public class TransactionDebitCommitted : TransactionEvent
    {
        public TransactionDebitCommitted(object aggregateRootId, TransactionInfo info)
            : base(aggregateRootId, info) { }
    }

    public class TransactionCreditCommitted : TransactionEvent
    {
        public TransactionCreditCommitted(object aggregateRootId, TransactionInfo info)
            : base(aggregateRootId, info) { }
    }

    public class TransactionCompleted : TransactionEvent
    {
        public TransactionCompleted(object aggregateRootId, TransactionInfo info)
            : base(aggregateRootId, info) { }
    }
}