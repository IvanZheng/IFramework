using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using IFramework.Domain;

namespace IFramework.Repositories
{
    public interface IDbContext
    {
        void RemoveEntity<TEntity>(TEntity entity)
            where TEntity : class;

        void Reload<TEntity>(TEntity entity)
            where TEntity : class;

        Task ReloadAsync<TEntity>(TEntity entity)
            where TEntity : class;


        void LoadReference<TEntity, TEntityProperty>(TEntity entity, Expression<Func<TEntity, TEntityProperty>> expression)
            where TEntity : class
            where TEntityProperty : class;

        Task LoadReferenceAsync<TEntity, TEntityProperty>(TEntity entity, Expression<Func<TEntity, TEntityProperty>> expression)
            where TEntity : class
            where TEntityProperty : class;

        void LoadCollection<TEntity, TEntityProperty>(TEntity entity, Expression<Func<TEntity, IEnumerable<TEntityProperty>>> expression)
            where TEntity : class
            where TEntityProperty : class;

        Task LoadCollectionAsync<TEntity, TEntityProperty>(TEntity entity, Expression<Func<TEntity, IEnumerable<TEntityProperty>>> expression)
            where TEntity : class
            where TEntityProperty : class;
    }
}
