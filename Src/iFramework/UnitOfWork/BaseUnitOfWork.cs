using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace IFramework.UnitOfWork
{
    public abstract class BaseUnitOfWork : IUnitOfWork
    {
        public virtual void Dispose() { }

        public abstract void Rollback();
        //public static readonly Type UnitOfWorkLifetimeManagerType = GetUnitOfWorkLifetimeManagerType();
        //static Type GetUnitOfWorkLifetimeManagerType()
        //{
        //    Type type = null;
        //    var unitOfWorkRegistration = IoCFactory.Instance.CurrentContainer.Registrations.FirstOrDefault(r => r.RegisteredType == typeof(IUnitOfWork));
        //    if (unitOfWorkRegistration != null)
        //    {
        //        type = unitOfWorkRegistration.LifetimeManagerType;
        //    }
        //    return type;
        //}

        //protected IMessageStore MessageStore
        //{
        //    get;
        //    private set;
        //}

        //public BaseUnitOfWork(IEventBus domainEventBus, IMessageStore messageStore)
        // {
        //EventBus = domainEventBus;
        //MessageStore = messageStore;
        // }

        #region IUnitOfWork Members

        //protected IEventBus EventBus
        //{
        //    get;
        //    private set;
        //}

        public abstract void Commit(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
                                    TransactionScopeOption scopOption = TransactionScopeOption.Required);

        public abstract Task CommitAsync(CancellationToken cancellationToken,
                                         IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
                                         TransactionScopeOption scopeOption = TransactionScopeOption.Required);

        public abstract Task CommitAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
                                         TransactionScopeOption scopOption = TransactionScopeOption.Required);

        #endregion
    }
}