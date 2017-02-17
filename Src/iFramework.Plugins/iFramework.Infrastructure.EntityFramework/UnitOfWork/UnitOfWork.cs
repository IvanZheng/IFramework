using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Entity;
using System.Transactions;
using IFramework.UnitOfWork;
using IFramework.Bus;
using IFramework.Infrastructure;
using IFramework.Config;
using IFramework.Repositories;
using IFramework.Domain;
using IFramework.Event;
using IFramework.Message;
using System.Data.Entity.Infrastructure;
using System.Threading.Tasks;
using IFramework.Infrastructure.Logging;
using System.Data.Entity.Validation;

namespace IFramework.EntityFramework
{
    public class UnitOfWork : IUnitOfWork
    {
        protected List<MSDbContext> _dbContexts;
        protected IEventBus _eventBus;
        protected ILogger _logger;
        // IEventPublisher _eventPublisher;

        public UnitOfWork(IEventBus eventBus, ILoggerFactory loggerFactory)//,  IEventPublisher eventPublisher, IMessageStore messageStore*/)
        {
            _dbContexts = new List<MSDbContext>();
            _eventBus = eventBus;
            _logger = loggerFactory.Create(this.GetType().Name);
            //  _eventPublisher = eventPublisher;
        }
        #region IUnitOfWork Members

        protected virtual void BeforeCommit()
        {

        }

        protected virtual void AfterCommit()
        {

        }

        public virtual void Commit()
        {
            try
            {
                using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required,
                                                           new TransactionOptions { IsolationLevel = IsolationLevel.ReadUncommitted },
                                                           TransactionScopeAsyncFlowOption.Enabled))
                {
                    _dbContexts.ForEach(dbContext =>
                    {
                        dbContext.SaveChanges();
                        dbContext.ChangeTracker.Entries().ForEach(e =>
                        {
                            if (e.Entity is AggregateRoot)
                            {
                                _eventBus.Publish((e.Entity as AggregateRoot).GetDomainEvents());
                            }
                        });
                    });
                    BeforeCommit();
                    scope.Complete();
                }
                AfterCommit();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                Rollback();
                throw new System.Data.OptimisticConcurrencyException(ex.Message, ex);
            }
            catch (DbEntityValidationException ex)
            {
                var errorMessage = string.Join(";", ex.EntityValidationErrors
                                                      .SelectMany(eve => eve.ValidationErrors
                                                                            .Select(e => new { Entry = eve.Entry, Error = e })
                                                      .Select(e => $"{e.Entry?.Entity?.GetType().Name}:{e.Error?.PropertyName} / {e.Error?.ErrorMessage}")));
                throw new Exception(errorMessage, ex);
            }
        }

        public async virtual Task CommitAsync()
        {

            try
            {
                using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required,
                                                             new TransactionOptions { IsolationLevel = IsolationLevel.ReadUncommitted },
                                                             TransactionScopeAsyncFlowOption.Enabled))
                {
                    foreach (var dbContext in _dbContexts)
                    {
                        await dbContext.SaveChangesAsync().ConfigureAwait(false);
                        dbContext.ChangeTracker.Entries().ForEach(e =>
                        {
                            if (e.Entity is AggregateRoot)
                            {
                                _eventBus.Publish((e.Entity as AggregateRoot).GetDomainEvents());
                            }
                        });
                    }
                    BeforeCommit();
                    scope.Complete();
                }
                AfterCommit();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                Rollback();
                throw new System.Data.OptimisticConcurrencyException(ex.Message, ex);
            }
            catch (DbEntityValidationException ex)
            {
                var errorMessage = string.Join(";", ex.EntityValidationErrors
                                                      .SelectMany(eve => eve.ValidationErrors
                                                                            .Select(e => new { Entry = eve.Entry, Error = e })
                                                      .Select(e => $"{e.Entry?.Entity?.GetType().Name}:{e.Error?.PropertyName} / {e.Error?.ErrorMessage}")));
                throw new Exception(errorMessage, ex);
            }
        }

        internal void RegisterDbContext(MSDbContext dbContext)
        {
            if (!_dbContexts.Exists(dbCtx => dbCtx.Equals(dbContext)))
            {
                _dbContexts.Add(dbContext);
            }
        }

        #endregion

        public void Dispose()
        {
            _dbContexts.ForEach(_dbCtx => _dbCtx.Dispose());
        }

        public void Rollback()
        {
            _dbContexts.ForEach(dbCtx =>
            {
                dbCtx.Rollback();
            });
            _eventBus.ClearMessages();
        }
    }
}
