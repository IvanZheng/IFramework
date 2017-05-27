using System;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Linq;
using IFramework.Domain;
using IFramework.Infrastructure;
using System.Data.Entity.Validation;
using System.Threading;
using System.Threading.Tasks;
using EntityState = System.Data.Entity.EntityState;

namespace IFramework.EntityFramework
{
    public static class QueryableCollectionInitializer
    {
        public static void InitializeQueryableCollections(this MSDbContext context, object entity)
        {
            var dbEntity = entity as Entity;
            if (dbEntity != null)
                ((dynamic) dbEntity).DomainContext = context;
        }
    }

    public class MSDbContext : DbContext
    {
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
            var objectContext = (this as IObjectContextAdapter).ObjectContext;
            if (objectContext != null)
                objectContext.ObjectMaterialized +=
                    (s, e) => this.InitializeQueryableCollections(e.Entity);
        }

        public virtual void Rollback()
        {
            var context = (this as IObjectContextAdapter).ObjectContext;
            ChangeTracker.Entries().Where(e => e.State == EntityState.Added || e.State == EntityState.Deleted)
                .ForEach(e => { e.State = EntityState.Detached; });
            var refreshableObjects = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Modified || e.State == EntityState.Unchanged)
                .Select(c => c.Entity);
            context.Refresh(RefreshMode.StoreWins, refreshableObjects);
            ChangeTracker.Entries().ForEach(e =>
            {
                (e.Entity as AggregateRoot)?.Rollback();
            });
        }



        public override int SaveChanges()
        {
            try
            {
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
                        .Select(e => new { eve.Entry, Error = e })
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
                        .Select(e => new { eve.Entry, Error = e })
                        .Select(
                            e => $"{e.Entry?.Entity?.GetType().Name}:{e.Error?.PropertyName} / {e.Error?.ErrorMessage}")));
                throw new Exception(errorMessage, ex);
            }
        }
    }
}