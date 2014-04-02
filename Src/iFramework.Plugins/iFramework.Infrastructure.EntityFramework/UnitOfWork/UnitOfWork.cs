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

namespace IFramework.EntityFramework
{
    public class UnitOfWork : BaseUnitOfWork
    {
        protected List<DbContext> _DbContexts;

        public UnitOfWork(IDomainEventBus eventBus)
            : base(eventBus)
        {
            _DomainEventBus = eventBus;
            _DbContexts = new List<DbContext>();
        }
        #region IUnitOfWork Members

        public override void Commit()
        {
            // TODO: should make domain events never losed, need transaction between
            //       model context and message queue, but need transaction across different scopes.
            try
            {
                _DbContexts.ForEach(dbContext => dbContext.SaveChanges());
                _TransactionScope.Complete();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                _TransactionScope.Dispose();
            }

            if (_DomainEventBus != null)
            {
                _DomainEventBus.Commit();
            }
            if (Configuration.IsPersistanceMessage)
            {
                // TODO: persistance command and domain events
                var currentCommandContext = PerMessageContextLifetimeManager.CurrentMessageContext;
                var domainEventContexts = _DomainEventBus.GetMessageContexts();
                MessageStore.Save(currentCommandContext, domainEventContexts);
            }
        }

        internal void RegisterDbContext(DbContext dbContext)
        {
            if (!_DbContexts.Exists(dbCtx => dbCtx == dbContext))
            {
                _DbContexts.Add(dbContext);
            }
        }

        #endregion


    }
}
