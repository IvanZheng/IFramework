using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IFramework.Infrastructure;
using IFramework.Repositories;


#if NET6_0_OR_GREATER
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
#else
using Microsoft.EntityFrameworkCore.Internal;
#endif


namespace IFramework.Domain
{
    public static class PocoContextInitializer
    {
        public static void InitializeMaterializer(this IDbContext context, object entity)
        {
            (entity as Entity)?.SetDomainContext(context);
        }

        public static void InitializeMaterializer(this Entity entity, object context)
        {
            if (context is IDbContext dbContext)
            {
                entity.SetDomainContext(dbContext);
            }
        }
    }

    public class Entity : IEntity
    {
        private IDbContext _dbContext;
        protected IDbContext DbContext
        {
            get
            {
                return _dbContext ??= this.GetPropertyValue<LazyLoader>(nameof(LazyLoader))?.GetPropertyValue<IDbContext>("Context");
            }
            set => _dbContext = value;
        }

        internal void SetDomainContext(IDbContext domainContext)
        {
            DbContext = domainContext;
        }

        public void ClearCollection<TEntity>(ICollection<TEntity> collection)
            where TEntity : class
        {
            var entities = collection.ToList();
            collection.Clear();
            entities.ForEach(e => DbContext?.RemoveEntity(e));
        }

        public void RemoveCollectionEntities<TEntity>(ICollection<TEntity> collection, params TEntity[] entities)
            where TEntity : class
        {
            entities?.ForEach(e =>
            {
                collection.Remove(e);
                DbContext?.RemoveEntity(e);
            });
        }

        public void Reload()
        {
            if (DbContext == null)
            {
                throw new NullReferenceException(nameof(DbContext));
            }

            DbContext.Reload(this);
            (this as AggregateRoot)?.Rollback();
        }

        public async Task ReloadAsync()
        {
            if (DbContext == null)
            {
                throw new NullReferenceException(nameof(DbContext));
            }
            await DbContext.ReloadAsync(this)
                           .ConfigureAwait(false);
            (this as AggregateRoot)?.Rollback();
        }

        public TContext GetDbContext<TContext>() where TContext : class
        {
            return DbContext as TContext;
        }
    }
}