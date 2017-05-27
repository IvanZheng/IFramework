using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using IFramework.Domain;
using IFramework.Event;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Logging;
using IFramework.UnitOfWork;

namespace IFramework.EntityFramework
{
    public class UnitOfWork : IUnitOfWork
    {
        protected List<MSDbContext> _dbContexts;
        protected IEventBus _eventBus;

        protected ILogger _logger;
        // IEventPublisher _eventPublisher;

        public UnitOfWork(IEventBus eventBus,
            ILoggerFactory loggerFactory) //,  IEventPublisher eventPublisher, IMessageStore messageStore*/)
        {
            _dbContexts = new List<MSDbContext>();
            _eventBus = eventBus;
            _logger = loggerFactory.Create(GetType().Name);
            //  _eventPublisher = eventPublisher;
        }

        public void Dispose()
        {
            _dbContexts.ForEach(_dbCtx => _dbCtx.Dispose());
        }

        public void Rollback()
        {
            _dbContexts.ForEach(dbCtx => { dbCtx.Rollback(); });
            _eventBus.ClearMessages();
        }

        #region IUnitOfWork Members

        protected virtual void BeforeCommit()
        {
        }

        protected virtual void AfterCommit()
        {
        }

        public virtual void Commit(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            TransactionScopeOption scopOption = TransactionScopeOption.Required)
        {
            using (var scope = new TransactionScope(scopOption,
                new TransactionOptions {IsolationLevel = isolationLevel},
                TransactionScopeAsyncFlowOption.Enabled))
            {
                _dbContexts.ForEach(dbContext =>
                {
                    dbContext.SaveChanges();
                    dbContext.ChangeTracker.Entries().ForEach(e =>
                    {
                        if (e.Entity is AggregateRoot)
                            _eventBus.Publish((e.Entity as AggregateRoot).GetDomainEvents());
                    });
                });
                BeforeCommit();
                scope.Complete();
            }
            AfterCommit();
        }

        public Task CommitAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            TransactionScopeOption scopeOption = TransactionScopeOption.Required)
        {
            return CommitAsync(CancellationToken.None, isolationLevel, scopeOption);
        }

        public virtual async Task CommitAsync(CancellationToken cancellationToken,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            TransactionScopeOption scopOption = TransactionScopeOption.Required)
        {
            using (var scope = new TransactionScope(scopOption,
                new TransactionOptions {IsolationLevel = isolationLevel},
                TransactionScopeAsyncFlowOption.Enabled))
            {
                foreach (var dbContext in _dbContexts)
                {
                    await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    dbContext.ChangeTracker.Entries().ForEach(e =>
                    {
                        if (e.Entity is AggregateRoot)
                            _eventBus.Publish((e.Entity as AggregateRoot).GetDomainEvents());
                    });
                }
                BeforeCommit();
                scope.Complete();
            }
            AfterCommit();
        }

        internal void RegisterDbContext(MSDbContext dbContext)
        {
            if (!_dbContexts.Exists(dbCtx => dbCtx.Equals(dbContext)))
                _dbContexts.Add(dbContext);
        }

        #endregion
    }
}