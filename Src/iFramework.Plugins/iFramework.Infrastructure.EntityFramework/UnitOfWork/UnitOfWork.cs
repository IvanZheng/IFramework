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
    public class UnitOfWork : BaseUnitOfWork
    {
        List<DbContext> _dbContexts;
        IEventPublisher _eventPublisher;

        public UnitOfWork(IDomainEventBus eventBus,  IEventPublisher eventPublisher, IMessageStore messageStore)
            : base(eventBus, messageStore)
        {
            _dbContexts = new List<DbContext>();
            _eventPublisher = eventPublisher;
        }
        #region IUnitOfWork Members

        public override void Commit()
        {
            // TODO: should make domain events never losed, need transaction between
            //       model context and message queue, but need transaction across different scopes.
            TransactionScope scope = new TransactionScope();
            bool success = false;
            IEnumerable<IMessageContext> domainEventContexts = null;
            try
            {
                var currentCommandContext = PerMessageContextLifetimeManager.CurrentMessageContext;
                IEnumerable<IDomainEvent> domainEvents = null;
                if (DomainEventBus != null)
                {
                    domainEvents = DomainEventBus.GetMessages();
                }
                _dbContexts.ForEach(dbContext => dbContext.SaveChanges());
                if (MessageStore != null)
                {
                    domainEventContexts = MessageStore.SaveCommand(currentCommandContext, domainEvents);
                }
                scope.Complete();
                success = true;
            }
            catch (Exception ex)
            {
                success = false;
                if (ex is DbUpdateConcurrencyException)
                {
                    _dbContexts.ForEach(dbCtx => {
                        dbCtx.ChangeTracker.Entries().ForEach(e => 
                        {
                            if (e.State == EntityState.Modified || e.State == EntityState.Deleted)
                            {
                                e.Reload();
                                e.State = EntityState.Unchanged;
                            }
                            else if (e.State == EntityState.Added)
                            {
                                e.State = EntityState.Detached;
                            }
                        });
                    });
                    DomainEventBus.ClearMessages();
                    //(ex as DbUpdateConcurrencyException).Entries.ForEach(e => e.Reload());
                    throw new System.Data.OptimisticConcurrencyException(ex.Message, ex);
                }
                else
                {
                    throw;
                }

            }
            finally
            {
                scope.Dispose();
                if (success)
                {
                    if (_eventPublisher != null)
                    {
                        _eventPublisher.Publish(domainEventContexts.ToArray());
                    }
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


    }
}
