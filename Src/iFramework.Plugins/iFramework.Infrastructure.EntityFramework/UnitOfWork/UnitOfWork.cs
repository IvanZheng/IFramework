using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFramework.UnitOfWork;
using IFramework.Bus;
using Microsoft.Practices.Unity;
using System.Data.Entity;
using System.Transactions;
using IFramework.Infrastructure;
using IFramework.Config;
using IFramework.Repositories;
using IFramework.Domain;
using IFramework.Event;
using IFramework.Infrastructure.Unity.LifetimeManagers;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Core;
using IFramework.Message;

namespace IFramework.EntityFramework
{
    public class UnitOfWork : IUnitOfWork
    {
        List<DbContext> _dbContexts;
        // IEventPublisher _eventPublisher;

        public UnitOfWork(/*IEventBus eventBus,  IEventPublisher eventPublisher, IMessageStore messageStore*/)
        //: base(eventBus, messageStore)
        {
            _dbContexts = new List<DbContext>();
            //  _eventPublisher = eventPublisher;
        }
        #region IUnitOfWork Members

        public void Commit()
        {
            using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required,
                                                             new TransactionOptions { IsolationLevel = System.Transactions.IsolationLevel.ReadUncommitted }))
            {
                try
                {
                    _dbContexts.ForEach(dbContext => dbContext.SaveChanges());
                    scope.Complete();
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _dbContexts.ForEach(dbCtx =>
                    {
                        dbCtx.ChangeTracker.Entries().ForEach(e =>
                        {
                            e.State = EntityState.Detached;
                        });
                    });
                    throw new System.Data.OptimisticConcurrencyException(ex.Message, ex);
                }
            }
        }

        internal void RegisterDbContext(DbContext dbContext)
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
    }
}
