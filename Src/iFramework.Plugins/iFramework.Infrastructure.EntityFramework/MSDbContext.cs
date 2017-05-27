using System;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IFramework.Domain;
using IFramework.Infrastructure;
using OptimisticConcurrencyException = System.Data.OptimisticConcurrencyException;

namespace IFramework.EntityFramework
{
    public static class QueryableCollectionInitializer
    {
        public static void InitializeQueryableCollections(this MSDbContext context, object entity)
        {
            (entity as Entity)?.SetDomainContext(context);
        }
    }

    public class MSDbContext : DbContext
    {
        private ObjectContext _objectContext;

        public MSDbContext(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {
            InitObjectContext();

            //if ((BaseUnitOfWork.UnitOfWorkLifetimeManagerType == typeof(PerMessageContextLifetimeManager) 
            //        && PerMessageContextLifetimeManager.CurrentMessageContext != null)
            //    || (BaseUnitOfWork.UnitOfWorkLifetimeManagerType == typeof(PerExecutionContextLifetimeManager)
            //        && (PerExecutionContextLifetimeManager.CurrentHttpContext != null || OperationContext.Current != null))
            //    || (BaseUnitOfWork.UnitOfWorkLifetimeManagerType == typeof(PerMessageOrExecutionContextLifetimeManager)
            //        && (PerMessageContextLifetimeManager.CurrentMessageContext != null
            //            || PerExecutionContextLifetimeManager.CurrentHttpContext != null 
            //            || OperationContext.Current != null)))
            //{
            //    var unitOfWork = (IoCFactory.Resolve<IUnitOfWork>() as UnitOfWork);
            //    unitOfWork.RegisterDbContext(this);
            //}
        }

        protected void InitObjectContext()
        {
            _objectContext = (this as IObjectContextAdapter).ObjectContext;
            if (_objectContext != null)
                _objectContext.ObjectMaterialized +=
                    (s, e) => this.InitializeQueryableCollections(e.Entity);
        }

        public virtual void Rollback()
        {
            ChangeTracker.Entries().Where(e => e.State == EntityState.Added || e.State == EntityState.Deleted)
                .ForEach(e => { e.State = EntityState.Detached; });
            var refreshableObjects = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Modified || e.State == EntityState.Unchanged)
                .Select(c => c.Entity);
            _objectContext.Refresh(RefreshMode.StoreWins, refreshableObjects);
            ChangeTracker.Entries().ForEach(e => { (e.Entity as AggregateRoot)?.Rollback(); });
        }

        public EntityKey GetEntityKey<T>(T entity)
            where T : class
        {
            ObjectStateEntry ose;
            if (null != entity && _objectContext.ObjectStateManager
                    .TryGetObjectStateEntry(entity, out ose))
                return ose.EntityKey;
            return null;
        }

        public void Reload<TEntity>(TEntity entity)
            where TEntity : class
        {
            var entry = Entry(entity);
            entry.Reload();
        }

        public override int SaveChanges()
        {
            try
            {
                ChangeTracker.Entries()
                    .Where(e => e.State == EntityState.Added)
                    .ForEach(e => { this.InitializeQueryableCollections(e.Entity); });
                return base.SaveChanges();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                Rollback();
                throw new OptimisticConcurrencyException(ex.Message, ex);
            }
            catch (DbEntityValidationException ex)
            {
                var errorMessage = string.Join(";", ex.EntityValidationErrors
                    .SelectMany(eve => eve.ValidationErrors
                        .Select(e => new {eve.Entry, Error = e})
                        .Select(
                            e => $"{e.Entry?.Entity?.GetType().Name}:{e.Error?.PropertyName} / {e.Error?.ErrorMessage}")));
                throw new Exception(errorMessage, ex);
            }
        }

        public override Task<int> SaveChangesAsync()
        {
            return SaveChangesAsync(CancellationToken.None);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            try
            {
                ChangeTracker.Entries()
                    .Where(e => e.State == EntityState.Added)
                    .ForEach(e => { this.InitializeQueryableCollections(e.Entity); });
                return await base.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                Rollback();
                throw new OptimisticConcurrencyException(ex.Message, ex);
            }
            catch (DbEntityValidationException ex)
            {
                var errorMessage = string.Join(";", ex.EntityValidationErrors
                    .SelectMany(eve => eve.ValidationErrors
                        .Select(e => new {eve.Entry, Error = e})
                        .Select(
                            e => $"{e.Entry?.Entity?.GetType().Name}:{e.Error?.PropertyName} / {e.Error?.ErrorMessage}")));
                throw new Exception(errorMessage, ex);
            }
        }
    }
}