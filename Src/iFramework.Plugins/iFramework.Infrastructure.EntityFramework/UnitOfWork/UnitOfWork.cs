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

namespace IFramework.EntityFramework
{
    public class UnitOfWork : IUnitOfWork
    {
        List<MSDbContext> _dbContexts;
        IEventBus _eventBus;
        // IEventPublisher _eventPublisher;

        public UnitOfWork(IEventBus eventBus)//,  IEventPublisher eventPublisher, IMessageStore messageStore*/)
        {
            _dbContexts = new List<MSDbContext>();
            _eventBus = eventBus;
            //  _eventPublisher = eventPublisher;
        }
        #region IUnitOfWork Members

        public void Commit()
        {
            using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required,
                                                             new TransactionOptions { IsolationLevel = System.Transactions.IsolationLevel.ReadUncommitted }, TransactionScopeAsyncFlowOption.Enabled))
            {
                try
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
                    scope.Complete();
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    Rollback();
                    throw new System.Data.OptimisticConcurrencyException(ex.Message, ex);
                }
            }
        }

        public async Task CommitAsync()
        {
            using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required,
                                                             new TransactionOptions { IsolationLevel = IsolationLevel.ReadUncommitted },
                                                             TransactionScopeAsyncFlowOption.Enabled))
            {
                try
                {
                    foreach (var dbContext in _dbContexts)
                    {
                        await dbContext.SaveChangesAsync();
                        dbContext.ChangeTracker.Entries().ForEach(e =>
                        {
                            if (e.Entity is AggregateRoot)
                            {
                                _eventBus.Publish((e.Entity as AggregateRoot).GetDomainEvents());
                            }
                        });
                    }
                    scope.Complete();
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    Rollback();
                    throw new System.Data.OptimisticConcurrencyException(ex.Message, ex);
                }
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
