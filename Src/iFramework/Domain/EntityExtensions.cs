using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using IFramework.Repositories;

namespace IFramework.Domain
{
    public static class EntityExtensions
    {
        public static void LoadReference<TEntity, TEntityProperty>(this TEntity entity, Expression<Func<TEntity, TEntityProperty>> expression)
            where TEntity : Entity
            where TEntityProperty : class
        {
            entity.GetDbContext<IDbContext>()?.LoadReference(entity, expression);
        }

        public static Task LoadReferenceAsync<TEntity, TEntityProperty>(this TEntity entity, Expression<Func<TEntity, TEntityProperty>> expression)
            where TEntity : Entity
            where TEntityProperty : class
        {
            return entity.GetDbContext<IDbContext>()?.LoadReferenceAsync(entity, expression);
        }

        public static void LoadCollection<TEntity, TEntityProperty>(this TEntity entity,
                                                          Expression<Func<TEntity, IEnumerable<TEntityProperty>>> expression)
            where TEntity : Entity
            where TEntityProperty : class
        {
            entity.GetDbContext<IDbContext>()?.LoadCollection(entity, expression);
        }

        public static Task LoadCollectionAsync<TEntity, TEntityProperty>(this TEntity entity,
                                                               Expression<Func<TEntity, IEnumerable<TEntityProperty>>> expression)
            where TEntity : Entity
            where TEntityProperty : class
        {
            return entity.GetDbContext<IDbContext>()?.LoadCollectionAsync(entity, expression);
        }
    }
}