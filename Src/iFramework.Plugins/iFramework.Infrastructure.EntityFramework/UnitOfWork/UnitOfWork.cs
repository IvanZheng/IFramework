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
using IFramework.MessageQueue.MessageFormat;
using IFramework.Message;

namespace IFramework.EntityFramework
{
    public class UnitOfWork : BaseUnitOfWork
    {
        protected List<DbContext> _dbContexts;

        public UnitOfWork(IDomainEventBus eventBus, IMessageStore messageStore)
            : base(eventBus, messageStore)
        {
            _dbContexts = new List<DbContext>();
        }
        #region IUnitOfWork Members

        public override void Commit()
        {
            // TODO: should make domain events never losed, need transaction between
            //       model context and message queue, but need transaction across different scopes.
            TransactionScope scope = new TransactionScope();
            try
            {
                _dbContexts.ForEach(dbContext => dbContext.SaveChanges());

                var currentCommandContext = PerMessageContextLifetimeManager.CurrentMessageContext;
                var domainEventContexts = new List<IMessageContext>();
                _domainEventBus.GetMessages().ForEach(domainEvent => {
                    domainEventContexts.Add(new MessageContext(domainEvent));
                });
                _messageStore.SaveCommand(currentCommandContext, domainEventContexts);

                scope.Complete();
            }
            catch (Exception ex)
            {
                if (ex is DbUpdateConcurrencyException)
                {
                    throw new System.Data.OptimisticConcurrencyException(ex.Message, ex);
                }
                else
                {
                    throw ex;
                }
            }
            finally
            {
                scope.Dispose();
            }

            if (_domainEventBus != null)
            {
                _domainEventBus.Commit();
            }
        }

        internal void RegisterDbContext(DbContext dbContext)
        {
            if (!_dbContexts.Exists(dbCtx => dbCtx == dbContext))
            {
                _dbContexts.Add(dbContext);
            }
        }

        #endregion


    }
}
