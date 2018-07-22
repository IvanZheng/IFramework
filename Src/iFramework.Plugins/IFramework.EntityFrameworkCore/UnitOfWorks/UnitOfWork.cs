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
    public class UnitOfWork : IUnitOfWork
    {
        protected List<MsDbContext> DbContexts;
        protected IEventBus EventBus;
        protected Exception Exception;
        protected ILogger Logger;

        public UnitOfWork(IEventBus eventBus,
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

        protected virtual void BeforeCommit() { }

        protected virtual void AfterCommit() { }

        public virtual void Commit(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
                                   TransactionScopeOption scopOption = TransactionScopeOption.Required)
        {
            try
            {
                using (var scope = new TransactionScope(scopOption,
                                                        new TransactionOptions {IsolationLevel = isolationLevel},
                                                        TransactionScopeAsyncFlowOption.Enabled))
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
                                     }
                                 });
                    });
                    BeforeCommit();
                    scope.Complete();
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
                                                        new TransactionOptions {IsolationLevel = isolationLevel},
                                                        TransactionScopeAsyncFlowOption.Enabled))
                {
                    foreach (var dbContext in DbContexts)
                    {
                        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                        dbContext.ChangeTracker.Entries()
                                 .ForEach(e =>
                                 {
                                     if (e.Entity is AggregateRoot)
                                     {
                                         EventBus.Publish((e.Entity as AggregateRoot).GetDomainEvents());
                                     }
                                 });
                    }
                    BeforeCommit();
                    scope.Complete();
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
                AfterCommit();
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