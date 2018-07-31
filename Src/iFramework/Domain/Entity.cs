
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using IFramework.Infrastructure;
using IFramework.Repositories;

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

    public class Entity: IEntity
    {
        protected IDbContext DomainContext { get; set; }

        internal void SetDomainContext(IDbContext domainContext)
        {
            DomainContext = domainContext;
        }

        public void ClearCollection<TEntity>(ICollection<TEntity> collection)
            where TEntity : class
        {
            var entities = collection.ToList();
            collection.Clear();
            entities.ForEach(e => DomainContext?.RemoveEntity(e));
        }

        public void RemoveCollectionEntities<TEntity>(ICollection<TEntity> collection, params TEntity[] entities)
            where TEntity : class
        {
            entities?.ForEach(e =>
            {
                collection.Remove(e);
                DomainContext?.RemoveEntity(e);
            });
        }

        public void Reload()
        {
            if (DomainContext == null)
            {
                throw new NullReferenceException(nameof(DomainContext));
            }
            DomainContext.Reload(this);
            (this as AggregateRoot)?.Rollback();
        }

        public async Task ReloadAsync()
        {
            if (DomainContext == null)
            {
                throw new NullReferenceException(nameof(DomainContext));
            }
            await DomainContext.ReloadAsync(this)
                               .ConfigureAwait(false);
            (this as AggregateRoot)?.Rollback();
        }

        public TContext GetDbContext<TContext>() where TContext : class
        {
            return DomainContext as TContext;
        }
    }
}