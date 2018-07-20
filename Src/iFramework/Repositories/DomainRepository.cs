using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using IFramework.DependencyInjection;
using IFramework.Domain;
using IFramework.Specifications;
using IFramework.UnitOfWork;

namespace IFramework.Repositories
{
    public class DomainRepository : IDomainRepository
    {
        private readonly IObjectProvider _objectProvider;

        #region Construct

        /// <summary>
        ///     Initializes a new instance of DomainRepository.
        /// </summary>
        /// <param name="objectProvider"></param>
        public DomainRepository(IObjectProvider objectProvider)
        {
            _objectProvider = objectProvider;
        }

        #endregion

        public IRepository<TAggregateRoot> GetRepository<TAggregateRoot>()
            where TAggregateRoot : class
        {
            return _objectProvider.GetService<IRepository<TAggregateRoot>>();
        }


        #region IRepository Members

        public void Add<TAggregateRoot>(IEnumerable<TAggregateRoot> entities)
            where TAggregateRoot : class
        {
            GetRepository<TAggregateRoot>().Add(entities);
        }

        public void Add<TAggregateRoot>(TAggregateRoot entity) where TAggregateRoot : class
        {
            GetRepository<TAggregateRoot>().Add(entity);
        }

        public TAggregateRoot GetByKey<TAggregateRoot>(params object[] keyValues)
            where TAggregateRoot : class
        {
            return GetRepository<TAggregateRoot>().GetByKey(keyValues);
        }

        public Task<TAggregateRoot> GetByKeyAsync<TAggregateRoot>(params object[] keyValues)
            where TAggregateRoot : class
        {
            return GetRepository<TAggregateRoot>().GetByKeyAsync(keyValues);
        }

        public long Count<TAggregateRoot>(ISpecification<TAggregateRoot> specification)
            where TAggregateRoot : class
        {
            return GetRepository<TAggregateRoot>().Count(specification);
        }

        public Task<long> CountAsync<TAggregateRoot>(ISpecification<TAggregateRoot> specification)
            where TAggregateRoot : class
        {
            return GetRepository<TAggregateRoot>().CountAsync(specification);
        }

        public long Count<TAggregateRoot>(Expression<Func<TAggregateRoot, bool>> specification)
            where TAggregateRoot : class
        {
            return GetRepository<TAggregateRoot>().Count(specification);
        }

        public Task<long> CountAsync<TAggregateRoot>(Expression<Func<TAggregateRoot, bool>> specification)
            where TAggregateRoot : class
        {
            return GetRepository<TAggregateRoot>().CountAsync(specification);
        }

        public IQueryable<TAggregateRoot> FindAll<TAggregateRoot>(params OrderExpression[] orderExpressions)
            where TAggregateRoot : class
        {
            return GetRepository<TAggregateRoot>().FindAll(orderExpressions);
        }

        public IQueryable<TAggregateRoot> FindAll<TAggregateRoot>(ISpecification<TAggregateRoot> specification,
                                                                  params OrderExpression[] orderExpressions) where TAggregateRoot : class
        {
            return GetRepository<TAggregateRoot>().FindAll(specification, orderExpressions);
        }

        public IQueryable<TAggregateRoot> FindAll<TAggregateRoot>(Expression<Func<TAggregateRoot, bool>> specification,
                                                                  params OrderExpression[] orderExpressions) where TAggregateRoot : class
        {
            return GetRepository<TAggregateRoot>().FindAll(specification, orderExpressions);
        }

        public TAggregateRoot Find<TAggregateRoot>(ISpecification<TAggregateRoot> specification)
            where TAggregateRoot : class
        {
            return GetRepository<TAggregateRoot>().Find(specification);
        }

        public Task<TAggregateRoot> FindAsync<TAggregateRoot>(ISpecification<TAggregateRoot> specification)
            where TAggregateRoot : class
        {
            return GetRepository<TAggregateRoot>().FindAsync(specification);
        }

        public TAggregateRoot Find<TAggregateRoot>(Expression<Func<TAggregateRoot, bool>> specification)
            where TAggregateRoot : class
        {
            return GetRepository<TAggregateRoot>().Find(specification);
        }

        public Task<TAggregateRoot> FindAsync<TAggregateRoot>(Expression<Func<TAggregateRoot, bool>> specification)
            where TAggregateRoot : class
        {
            return GetRepository<TAggregateRoot>().FindAsync(specification);
        }

        public bool Exists<TAggregateRoot>(ISpecification<TAggregateRoot> specification)
            where TAggregateRoot : class
        {
            return GetRepository<TAggregateRoot>().Exists(specification);
        }

        public Task<bool> ExistsAsync<TAggregateRoot>(ISpecification<TAggregateRoot> specification)
            where TAggregateRoot : class
        {
            return GetRepository<TAggregateRoot>().ExistsAsync(specification);
        }

        public bool Exists<TAggregateRoot>(Expression<Func<TAggregateRoot, bool>> specification)
            where TAggregateRoot : class
        {
            return GetRepository<TAggregateRoot>().Exists(specification);
        }

        public Task<bool> ExistsAsync<TAggregateRoot>(Expression<Func<TAggregateRoot, bool>> specification)
            where TAggregateRoot : class
        {
            return GetRepository<TAggregateRoot>().ExistsAsync(specification);
        }

        public void Remove<TAggregateRoot>(TAggregateRoot entity) where TAggregateRoot : class
        {
            GetRepository<TAggregateRoot>().Remove(entity);
        }

        public void Remove<TAggregateRoot>(IEnumerable<TAggregateRoot> entities)
            where TAggregateRoot : class
        {
            GetRepository<TAggregateRoot>().Remove(entities);
        }

        public void Update<TAggregateRoot>(TAggregateRoot entity) where TAggregateRoot : class
        {
            GetRepository<TAggregateRoot>().Update(entity);
        }

        public (IQueryable<TAggregateRoot>, long) PageFind<TAggregateRoot>(int pageIndex,
                                                                           int pageSize,
                                                                           Expression<Func<TAggregateRoot, bool>> specification,
                                                                           params OrderExpression[] orderExpressions)
            where TAggregateRoot : class
        {
            return GetRepository<TAggregateRoot>().PageFind(pageIndex, pageSize, specification, orderExpressions);
        }


        public Task<(IQueryable<TAggregateRoot>, long)> PageFindAsync<TAggregateRoot>(int pageIndex,
                                                                                      int pageSize,
                                                                                      Expression<Func<TAggregateRoot, bool>> specification,
                                                                                      params OrderExpression[] orderExpressions)
            where TAggregateRoot : class
        {
            return GetRepository<TAggregateRoot>().PageFindAsync(pageIndex, pageSize, specification, orderExpressions);
        }

        public (IQueryable<TAggregateRoot>, long) PageFind<TAggregateRoot>(int pageIndex,
                                                                           int pageSize,
                                                                           ISpecification<TAggregateRoot> specification,
                                                                           params OrderExpression[] orderExpressions) where TAggregateRoot : class
        {
            return GetRepository<TAggregateRoot>()
                .PageFind(pageIndex, pageSize, specification,
                          orderExpressions);
        }

        public Task<(IQueryable<TAggregateRoot>, long)> PageFindAsync<TAggregateRoot>(int pageIndex,
                                                                                      int pageSize,
                                                                                      ISpecification<TAggregateRoot> specification,
                                                                                      params OrderExpression[] orderExpressions)
            where TAggregateRoot : class
        {
            return GetRepository<TAggregateRoot>().PageFindAsync(pageIndex, pageSize, specification, orderExpressions);
        }

        public void Reload<TEntity>(TEntity entity) where TEntity : Entity
        {
            GetRepository<TEntity>().Reload(entity);
        }

        public Task ReloadAsync<TEntity>(TEntity entity) where TEntity : Entity
        {
            return GetRepository<TEntity>().ReloadAsync(entity);
        }

        #endregion
    }
}