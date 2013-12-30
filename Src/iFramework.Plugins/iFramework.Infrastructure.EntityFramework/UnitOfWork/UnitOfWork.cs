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

namespace IFramework.EntityFramework
{
    public class UnitOfWork : BaseUnitOfWork
    {
        [Dependency(Constants.Configuration.DomainModelContext)]
        public DbContext DomainModelContext { get; set; }


        public UnitOfWork(IDomainEventBus eventBus)
            : base(eventBus)
        {
            _DomainEventBus = eventBus;
        }
        #region IUnitOfWork Members

        public override void Commit()
        {

            // using (var scope = new TransactionScope())
            // {
            if (DomainModelContext != null)
            {
                DomainModelContext.SaveChanges();
            }
            if (_ModelContextCommitActions.Count > 0)
            {
                _ModelContextCommitActions.ForEach(a => a.Invoke());
            }
            base.Commit();
            //      scope.Complete();
            //  }
        }

        #endregion

        public override IRepository<TAggregateRoot> GetRepository<TAggregateRoot>()
        {
            try
            {

                var repository = IoCFactory.Resolve<IRepository<TAggregateRoot>>(new ParameterOverride("dbContext", this.DomainModelContext));
                return repository;
            }
            catch
            {
                throw;
            }
        }
    }
}
