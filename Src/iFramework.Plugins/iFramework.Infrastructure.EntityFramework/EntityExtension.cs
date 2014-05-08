using IFramework.Domain;
using IFramework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace IFramework.EntityFramework
{
    public static class EntityExtension
    {
        public static TContext GetDbContext<TContext>(this Entity entity) where TContext : class
        {
            TContext context = entity.GetValueByKey<TContext>("DomainContext");
            if (context == null)
            {
                context = IoCFactory.Resolve<TContext>() as TContext;
            }
            return context;
        }

        public static DbEntityEntry<TEntity> GetDbEntityEntry<TEntity>(this TEntity entity)
            where TEntity : Entity
        {
            return entity.GetDbContext<MSDbContext>().Entry<TEntity>(entity);
        }

        public static void MarkAsDeleted(this Entity entity)
        {
            entity.GetDbEntityEntry().State = EntityState.Deleted;
        }

        public static void MarkAsAdded(this Entity entity)
        {
            entity.GetDbEntityEntry().State = EntityState.Added;
        }

        public static void MarkAsModified(this Entity entity)
        {
            entity.GetDbEntityEntry().State = EntityState.Modified;
        }

        public static void MarkAsUnchanged(this Entity entity)
        {
            entity.GetDbEntityEntry().State = EntityState.Unchanged;
        }

        public static IQueryable<TElement> GetQueryable<TElement>(this Entity entity, string collectionName)
            where TElement : class
        {
            IQueryable<TElement> query = null;
            var entry = entity.GetDbEntityEntry();
            if (entry != null)
            {
                query = entry.Collection(collectionName).Query().Cast<TElement>();
            }
            return query;
        }

        public static IQueryable<TElement> GetQueryable<TEntity, TElement>(this TEntity entity, Expression<Func<TEntity, ICollection<TElement>>> navigationProperty)
            where TEntity : Entity
            where TElement : class
        {
            IQueryable<TElement> query = null;
            var entry = entity.GetDbEntityEntry();
            if (entry != null)
            {
                query = entry.Collection(navigationProperty).Query().Cast<TElement>();
            }
            return query;
        }

        public static void ReferenceLoad<TEntity, TProperty>(this TEntity entity, Expression<Func<TEntity, TProperty>> navigationProperty)
            where TEntity : Entity
            where TProperty : class
        {
            var entry = entity.GetDbEntityEntry();
            if (entry != null)
            {
                entity.GetDbEntityEntry().Reference(navigationProperty).Load();
            }
        }

        public static void ReferenceLoad<TProperty>(this Entity entity, string navigationPropertyName)
          
            where TProperty : class
        {
            var entry = entity.GetDbEntityEntry();
            if (entry != null)
            {
                entity.GetDbEntityEntry().Reference(navigationPropertyName).Load();
            }
        }

        public static void CollectionLoad<TEntity, TElement>(this TEntity entity, Expression<Func<TEntity, ICollection<TElement>>> navigationProperty)
            where TEntity : Entity
            where TElement : class
        {
            var entry = entity.GetDbEntityEntry<TEntity>();
            if (entry != null)
            {
                entity.GetDbEntityEntry().Collection(navigationProperty).Load();
            }
        }

        public static void CollectionLoad<TElement>(this Entity entity, string navigationPropertyName)
            where TElement : class
        {
            var entry = entity.GetDbEntityEntry();
            if (entry != null)
            {
                entity.GetDbEntityEntry().Collection(navigationPropertyName).Load();
            }
        }

        public static void RemoveEntity<T>(this ICollection<T> collection, T entity)
            where T : Entity
        {
            collection.Remove(entity);
            entity.MarkAsDeleted();
        }
    }
}
