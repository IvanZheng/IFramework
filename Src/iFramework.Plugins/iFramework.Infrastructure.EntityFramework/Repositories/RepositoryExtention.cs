using IFramework.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
    }
}
