using IFramework.Repositories;
using IFramework.Specifications;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.EntityFramework.Repositories
{
    public static class RepositoryExtention
    {
        public static void AddWithSaveChanges<TEntity>(this IRepository<TEntity> repository, TEntity entity)
            where TEntity : class
        {
            repository.Add(entity);
            (repository as Repository<TEntity>)._Container.SaveChanges();
        }

        public static void AddWithSaveChanges<TEntity>(this IDomainRepository repository, TEntity entity)
            where TEntity : class
        {
            (repository as DomainRepository).GetRepository<TEntity>().AddWithSaveChanges(entity);
        }

        public static Task<List<TEntity>> ToListAsync<TEntity>(this IQueryable<TEntity> query) where TEntity : class
        {
            return QueryableExtensions.ToListAsync(query);
        }
    }
}
