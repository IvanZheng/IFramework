using System;
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
    public class UnitOfWork: IUnitOfWork
    {
        protected List<MSDbContext> _dbContexts;
        protected IEventBus _eventBus;
        protected Exception _exception;
        protected ILogger _logger;

        public UnitOfWork(IEventBus eventBus,
                          ILoggerFactory loggerFactory)
        {
            _dbContexts = new List<MSDbContext>();
            _eventBus = eventBus;
            _logger = loggerFactory.Create(GetType().Name);
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
            try
            {
                using (var scope = new TransactionScope(scopOption,
                                                        new TransactionOptions { IsolationLevel = isolationLevel },
                                                        TransactionScopeAsyncFlowOption.Enabled))
                {
                    _dbContexts.ForEach(dbContext =>
                    {
                        dbContext.SaveChanges();
                        dbContext.ChangeTracker.Entries()
                                 .ForEach(e =>
                                 {
                                     if (e.Entity is AggregateRoot root)
                                         _eventBus.Publish(root.GetDomainEvents());
                                 });
                    });
                    BeforeCommit();
                    scope.Complete();
                }
            }
            catch (Exception ex)
            {
                _exception = ex;
                throw;
            }
            finally
            {
                AfterCommit();
            }

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
            try
            {
                using (var scope = new TransactionScope(scopOption,
                                                        new TransactionOptions { IsolationLevel = isolationLevel },
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
            }
            catch (Exception ex)
            {
                _exception = ex;
                throw;
            }
            finally
            {
                AfterCommit();
            }

        }

        internal void RegisterDbContext(MSDbContext dbContext)
        {
            if (!_dbContexts.Exists(dbCtx => dbCtx.Equals(dbContext)))
                _dbContexts.Add(dbContext);
        }

        #endregion
    }
}