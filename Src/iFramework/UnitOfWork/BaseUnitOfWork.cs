using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFramework.Bus;
using IFramework.Repositories;
using IFramework.Infrastructure;
using IFramework.Domain;
using IFramework.Event;
using IFramework.Message;
using IFramework.Config;
using System.Transactions;
using System.Threading.Tasks;

namespace IFramework.UnitOfWork
{
    public abstract class BaseUnitOfWork : IUnitOfWork
    {
        //public static readonly Type UnitOfWorkLifetimeManagerType = GetUnitOfWorkLifetimeManagerType();
        //static Type GetUnitOfWorkLifetimeManagerType()
        //{
        //    Type type = null;
        //    var unitOfWorkRegistration = IoCFactory.Instance.CurrentContainer.Registrations.FirstOrDefault(r => r.RegisteredType == typeof(IUnitOfWork));
        //    if (unitOfWorkRegistration != null)
        //    {
        //        type = unitOfWorkRegistration.LifetimeManagerType;
        //    }
        //    return type;
        //}

        //protected IMessageStore MessageStore
        //{
        //    get;
        //    private set;
        //}

        //public BaseUnitOfWork(IEventBus domainEventBus, IMessageStore messageStore)
        // {
        //EventBus = domainEventBus;
        //MessageStore = messageStore;
        // }
        #region IUnitOfWork Members

        //protected IEventBus EventBus
        //{
        //    get;
        //    private set;
        //}

        public abstract void Commit();

        public abstract Task CommitAsync();
        #endregion

        public virtual void Dispose()
        {
        }

        public abstract void Rollback();
    }
}
