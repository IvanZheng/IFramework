using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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
        protected MsDbContext DbContext => DbContexts.FirstOrDefault();

        protected UnitOfWorkBase(IEventBus eventBus,
                          ILoggerFactory loggerFactory)
        {
            DbContexts = new List<MsDbContext>();
            EventBus = eventBus;
            Logger = loggerFactory.CreateLogger(GetType());
        }

        public void Dispose()
        {
            DbContexts.ForEach(dbCtx => dbCtx.Dispose());
        }

        public virtual void Rollback()
        {
            DbContexts.ForEach(dbCtx => { dbCtx.Rollback(); });
            EventBus.ClearMessages(false);
        }

        #region IUnitOfWork Members

        protected virtual Task CommittingAsync()
        {
            return Task.CompletedTask;
        }

        protected virtual Task AfterCommitAsync()
        {
            return Task.CompletedTask;
        }

        protected virtual void DoCommit()
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
            CommittingAsync().Wait();
        }

        public virtual void Commit()
        {
            try
            {
                DbContext.ExecuteInTransaction(DoCommit);
            }
            catch (Exception ex)
            {
                Rollback();
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

        public Task CommitAsync()
        {
            return CommitAsync(CancellationToken.None);
        }

        protected virtual async Task DoCommitAsync(CancellationToken cancellationToken)
        {
            foreach (var dbContext in DbContexts)
            {
                dbContext.ChangeTracker.Entries()
                         .ForEach(e =>
                         {
                             if (e.Entity is AggregateRoot root)
                             {
                                 EventBus.Publish(root.GetDomainEvents());
                                 root.ClearDomainEvents();
                             }
                         });
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            await CommittingAsync().ConfigureAwait(false);
        }

        public virtual async Task CommitAsync(CancellationToken cancellationToken)
        {
            try
            {
                await DbContext.ExecuteInTransactionAsync(() => DoCommitAsync(cancellationToken), cancellationToken)
                               .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Rollback();
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
                if (DbContexts.Count > 0)
                {
                    throw new Exception("Only support one DbContext!");
                }
                DbContexts.Add(dbContext);
            }

        }

        #endregion
    }
}