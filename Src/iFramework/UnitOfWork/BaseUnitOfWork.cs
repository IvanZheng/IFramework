using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFramework.Bus;
using IFramework.Repositories;
using IFramework.Infrastructure;
using Microsoft.Practices.Unity;
using IFramework.Domain;
using IFramework.Event;
using IFramework.Infrastructure.Unity.LifetimeManagers;
using IFramework.Message;
using IFramework.Config;
using System.Transactions;

namespace IFramework.UnitOfWork
{
    public abstract class BaseUnitOfWork : IUnitOfWork
    {
        public static readonly Type UnitOfWorkLifetimeManagerType = GetUnitOfWorkLifetimeManagerType();
        static Type GetUnitOfWorkLifetimeManagerType()
        {
            Type type = null;
            var unitOfWorkRegistration = IoCFactory.Instance.CurrentContainer.Registrations.FirstOrDefault(r => r.RegisteredType == typeof(IUnitOfWork));
            if (unitOfWorkRegistration != null)
            {
                type = unitOfWorkRegistration.LifetimeManagerType;
            }
            return type;
        }

        protected IMessageStore MessageStore
        {
            get
            {
                return IoCFactory.Resolve<IMessageStore>();
            }
        }

        public BaseUnitOfWork(IDomainEventBus domainEventBus)
        {
            _domainEventBus = domainEventBus;
        }
        #region IUnitOfWork Members

        protected IDomainEventBus _domainEventBus;

        public abstract void Commit();

        #endregion

        public virtual void Dispose()
        {
        }
    }
}
