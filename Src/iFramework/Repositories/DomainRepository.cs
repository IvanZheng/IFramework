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

        public virtual void Add<TAggregateRoot>(IEnumerable<TAggregateRoot> entities)
            where TAggregateRoot : class
        {
            GetRepository<TAggregateRoot>().Add(entities);
        }

        public virtual void Add<TAggregateRoot>(TAggregateRoot entity) where TAggregateRoot : class
        {
            GetRepository<TAggregateRoot>().Add(entity);
        }

        public virtual TAggregateRoot GetByKey<TAggregateRoot>(params object[] keyValues)
            where TAggregateRoot : class
        {
            return GetRepository<TAggregateRoot>().GetByKey(keyValues);
        }

        public virtual Task<TAggregateRoot> GetByKeyAsync<TAggregateRoot>(params object[] keyValues)
            where TAggregateRoot : class
        {
            return GetRepository<TAggregateRoot>().GetByKeyAsync(keyValues);
        }

        public virtual long Count<TAggregateRoot>(ISpecification<TAggregateRoot> specification)
            where TAggregateRoot : class
        {
            return GetRepository<TAggregateRoot>().Count(specification);
        }

        public virtual Task<long> CountAsync<TAggregateRoot>(ISpecification<TAggregateRoot> specification)
            where TAggregateRoot : class
        {
            return GetRepository<TAggregateRoot>().CountAsync(specification);
        }

        public virtual long Count<TAggregateRoot>(Expression<Func<TAggregateRoot, bool>> specification)
            where TAggregateRoot : class
        {
            return GetRepository<TAggregateRoot>().Count(specification);
        }

        public virtual Task<long> CountAsync<TAggregateRoot>(Expression<Func<TAggregateRoot, bool>> specification)
            where TAggregateRoot : class
        {
            return GetRepository<TAggregateRoot>().CountAsync(specification);
        }

        public virtual IQueryable<TAggregateRoot> FindAll<TAggregateRoot>(params OrderExpression[] orderExpressions)
            where TAggregateRoot : class
        {
            return GetRepository<TAggregateRoot>().FindAll(orderExpressions);
        }

        public virtual IQueryable<TAggregateRoot> FindAll<TAggregateRoot>(ISpecification<TAggregateRoot> specification,
                                                                  params OrderExpression[] orderExpressions) where TAggregateRoot : class
        {
            return GetRepository<TAggregateRoot>().FindAll(specification, orderExpressions);
        }

        public virtual IQueryable<TAggregateRoot> FindAll<TAggregateRoot>(Expression<Func<TAggregateRoot, bool>> specification,
                                                                  params OrderExpression[] orderExpressions) where TAggregateRoot : class
        {
            return GetRepository<TAggregateRoot>().FindAll(specification, orderExpressions);
        }

        public virtual TAggregateRoot Find<TAggregateRoot>(ISpecification<TAggregateRoot> specification)
            where TAggregateRoot : class
        {
            return GetRepository<TAggregateRoot>().Find(specification);
        }

        public virtual Task<TAggregateRoot> FindAsync<TAggregateRoot>(ISpecification<TAggregateRoot> specification)
            where TAggregateRoot : class
        {
            return GetRepository<TAggregateRoot>().FindAsync(specification);
        }

        public virtual TAggregateRoot Find<TAggregateRoot>(Expression<Func<TAggregateRoot, bool>> specification)
            where TAggregateRoot : class
        {
            return GetRepository<TAggregateRoot>().Find(specification);
        }

        public virtual Task<TAggregateRoot> FindAsync<TAggregateRoot>(Expression<Func<TAggregateRoot, bool>> specification)
            where TAggregateRoot : class
        {
            return GetRepository<TAggregateRoot>().FindAsync(specification);
        }

        public virtual bool Exists<TAggregateRoot>(ISpecification<TAggregateRoot> specification)
            where TAggregateRoot : class
        {
            return GetRepository<TAggregateRoot>().Exists(specification);
        }

        public virtual Task<bool> ExistsAsync<TAggregateRoot>(ISpecification<TAggregateRoot> specification)
            where TAggregateRoot : class
        {
            return GetRepository<TAggregateRoot>().ExistsAsync(specification);
        }

        public virtual bool Exists<TAggregateRoot>(Expression<Func<TAggregateRoot, bool>> specification)
            where TAggregateRoot : class
        {
            return GetRepository<TAggregateRoot>().Exists(specification);
        }

        public virtual Task<bool> ExistsAsync<TAggregateRoot>(Expression<Func<TAggregateRoot, bool>> specification)
            where TAggregateRoot : class
        {
            return GetRepository<TAggregateRoot>().ExistsAsync(specification);
        }

        public virtual void Remove<TAggregateRoot>(TAggregateRoot entity) where TAggregateRoot : class
        {
            GetRepository<TAggregateRoot>().Remove(entity);
        }

        public virtual void Remove<TAggregateRoot>(IEnumerable<TAggregateRoot> entities)
            where TAggregateRoot : class
        {
            GetRepository<TAggregateRoot>().Remove(entities);
        }

        public virtual void Update<TAggregateRoot>(TAggregateRoot entity) where TAggregateRoot : class
        {
            GetRepository<TAggregateRoot>().Update(entity);
        }

        public virtual (IQueryable<TAggregateRoot>, long) PageFind<TAggregateRoot>(int pageIndex,
                                                                           int pageSize,
                                                                           Expression<Func<TAggregateRoot, bool>> specification,
                                                                           params OrderExpression[] orderExpressions)
            where TAggregateRoot : class
        {
            return GetRepository<TAggregateRoot>().PageFind(pageIndex, pageSize, specification, orderExpressions);
        }


        public virtual Task<(IQueryable<TAggregateRoot>, long)> PageFindAsync<TAggregateRoot>(int pageIndex,
                                                                                      int pageSize,
                                                                                      Expression<Func<TAggregateRoot, bool>> specification,
                                                                                      params OrderExpression[] orderExpressions)
            where TAggregateRoot : class
        {
            return GetRepository<TAggregateRoot>().PageFindAsync(pageIndex, pageSize, specification, orderExpressions);
        }

        public virtual (IQueryable<TAggregateRoot>, long) PageFind<TAggregateRoot>(int pageIndex,
                                                                           int pageSize,
                                                                           ISpecification<TAggregateRoot> specification,
                                                                           params OrderExpression[] orderExpressions) where TAggregateRoot : class
        {
            return GetRepository<TAggregateRoot>()
                .PageFind(pageIndex, pageSize, specification,
                          orderExpressions);
        }

        public virtual Task<(IQueryable<TAggregateRoot>, long)> PageFindAsync<TAggregateRoot>(int pageIndex,
                                                                                      int pageSize,
                                                                                      ISpecification<TAggregateRoot> specification,
                                                                                      params OrderExpression[] orderExpressions)
            where TAggregateRoot : class
        {
            return GetRepository<TAggregateRoot>().PageFindAsync(pageIndex, pageSize, specification, orderExpressions);
        }

        public virtual void Reload<TEntity>(TEntity entity) where TEntity : Entity
        {
            GetRepository<TEntity>().Reload(entity);
        }

        public virtual Task ReloadAsync<TEntity>(TEntity entity) where TEntity : Entity
        {
            return GetRepository<TEntity>().ReloadAsync(entity);
        }

        #endregion
    }
}