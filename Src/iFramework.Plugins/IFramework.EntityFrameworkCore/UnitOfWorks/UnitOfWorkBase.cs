using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using IFramework.Domain;
using IFramework.Event;
using IFramework.Infrastructure;
using IFramework.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using IsolationLevel = System.Transactions.IsolationLevel;

namespace IFramework.EntityFrameworkCore.UnitOfWorks
{
    public abstract class UnitOfWorkBase : IUnitOfWork
    {
        protected List<MsDbContext> DbContexts;
        protected IEventBus EventBus;
        protected Exception Exception;
        protected ILogger Logger;
        protected bool InTransaction => Transaction.Current != null;
        protected UnitOfWorkBase(IEventBus eventBus,
                          ILoggerFactory loggerFactory)
        {
            DbContexts = new List<MsDbContext>();
            EventBus = eventBus;
            Logger = loggerFactory.CreateLogger(GetType().Name);
        }

        public void Dispose()
        {
            DbContexts.ForEach(dbCtx => dbCtx.Dispose());
        }

        public void Rollback()
        {
            DbContexts.ForEach(dbCtx => { dbCtx.Rollback(); });
            EventBus.ClearMessages();
        }

        #region IUnitOfWork Members

        protected virtual Task BeforeCommitAsync()
        {
            return Task.CompletedTask;
        }

        protected virtual Task AfterCommitAsync()
        {
            return Task.CompletedTask;
        }

        public virtual void Commit(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
                                   TransactionScopeOption scopOption = TransactionScopeOption.Required)
        {
            try
            {
                void CommitAction()
                {
                    DbContexts.ForEach(dbContext =>
                    {
                        dbContext.SaveChanges();
                        dbContext.ChangeTracker.Entries()
                                 .ForEach(e =>
                                 {
                                     if (e.Entity is AggregateRoot root)
                                     {
                                         EventBus.Publish(root.GetDomainEvents());
                                         root.ClearDomainEvents();
                                     }
                                 });
                    });
                    BeforeCommitAsync().Wait();
                }

                if (InTransaction)
                {
                    CommitAction();
                }
                else
                {
                    using (var scope = new TransactionScope(scopOption,
                                                            new TransactionOptions { IsolationLevel = isolationLevel },
                                                            TransactionScopeAsyncFlowOption.Enabled))
                    {
                        CommitAction();
                        scope.Complete();
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is DbUpdateConcurrencyException)
                {
                    Exception = new DBConcurrencyException(ex.Message, ex);
                    throw Exception;
                }
                else
                {
                    Exception = ex;
                    throw;
                }
            }
            finally
            {
                AfterCommitAsync().Wait();
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
                async Task CommitFuncAsync()
                {
                    foreach (var dbContext in DbContexts)
                    {
                        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                        dbContext.ChangeTracker.Entries()
                                 .ForEach(e =>
                                 {
                                     if (e.Entity is AggregateRoot root)
                                     {
                                         EventBus.Publish(root.GetDomainEvents());
                                         root.ClearDomainEvents();
                                     }
                                 });
                    }
                    await BeforeCommitAsync().ConfigureAwait(false);
                }

                if (InTransaction)
                {
                    await CommitFuncAsync().ConfigureAwait(false);
                }
                else
                {
                    using (var scope = new TransactionScope(scopOption,
                                                            new TransactionOptions { IsolationLevel = isolationLevel },
                                                            TransactionScopeAsyncFlowOption.Enabled))
                    {
                        await CommitFuncAsync().ConfigureAwait(false);

                        scope.Complete();
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is DbUpdateConcurrencyException)
                {
                    Exception = new DBConcurrencyException(ex.Message, ex);
                    throw Exception;
                }
                else
                {
                    Exception = ex;
                }
                throw;
            }
            finally
            {
                await AfterCommitAsync().ConfigureAwait(false);
            }
        }

        internal void RegisterDbContext(MsDbContext dbContext)
        {
            if (!DbContexts.Exists(dbCtx => dbCtx.Equals(dbContext)))
            {
                DbContexts.Add(dbContext);
            }
        }

        #endregion
    }
}