using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using IFramework.Specifications;
using IFramework.UnitOfWork;

namespace IFramework.Repositories
{
    /// <summary>
    ///     Represents the base class for repositories.
    /// </summary>
    /// <typeparam name="TAggregateRoot">The type of the aggregate root.</typeparam>
    public abstract class BaseRepository<TAggregateRoot> : IRepository<TAggregateRoot>
        where TAggregateRoot : class
    {
        #region Construct

        protected BaseRepository(object dbContext, IUnitOfWork unitOfWork) { }

        #endregion

        #region Protected Methods

        /// <summary>
        ///     Adds an entity to the repository.
        /// </summary>
        /// <param name="entities"></param>
        protected abstract void DoAdd(IEnumerable<TAggregateRoot> entities);
        protected abstract void DoAdd(TAggregateRoot entity);
        protected abstract Task DoAddAsync(IEnumerable<TAggregateRoot> entities);
        protected abstract Task DoAddAsync(TAggregateRoot entity);

        /// <summary>
        ///     Gets the entity instance from repository by a given key.
        /// </summary>
        /// <param name="keyValues">The key of the entity.</param>
        /// <returns>The instance of the entity.</returns>
        protected abstract TAggregateRoot DoGetByKey(params object[] keyValues);

        protected abstract Task<TAggregateRoot> DoGetByKeyAsync(params object[] keyValues);


        /// <summary>
        ///     Finds all the aggregate roots from repository, sorting by using the provided sort predicate
        ///     and the specified sort order.
        /// </summary>
        /// <param name="orderExpressions">The sort predicate which is used for sorting.</param>
        /// <returns>
        ///     All the aggregate roots got from the repository, with the aggregate roots being sorted by
        ///     using the provided sort predicate and the sort order.
        /// </returns>
        protected virtual IQueryable<TAggregateRoot> DoFindAll(params OrderExpression[] orderExpressions)
        {
            return DoFindAll(null, orderExpressions);
        }

        /// <summary>
        ///     Finds all the aggregate roots that match the given specification, and sorts the aggregate roots
        ///     by using the provided sort predicate and the specified sort order.
        /// </summary>
        /// <param name="specification">The specification with which the aggregate roots should match.</param>
        /// <param name="orderExpressions">The sort predicate which is used for sorting.</param>
        /// <returns>
        ///     All the aggregate roots that match the given specification and were sorted by using the given sort predicate
        ///     and the sort order.
        /// </returns>
        protected abstract IQueryable<TAggregateRoot> DoFindAll(ISpecification<TAggregateRoot> specification,
                                                                params OrderExpression[] orderExpressions);

        /// <summary>
        ///     Finds a single aggregate root that matches the given specification.
        /// </summary>
        /// <param name="specification">The specification with which the aggregate root should match.</param>
        /// <returns>The instance of the aggregate root.</returns>
        protected abstract TAggregateRoot DoFind(ISpecification<TAggregateRoot> specification);

        protected abstract Task<TAggregateRoot> DoFindAsync(ISpecification<TAggregateRoot> specification);


        /// <summary>
        ///     Checkes whether the aggregate root which matches the given specification exists.
        /// </summary>
        /// <param name="specification">The specification with which the aggregate root should match.</param>
        /// <returns>True if the aggregate root exists, otherwise false.</returns>
        protected abstract bool DoExists(ISpecification<TAggregateRoot> specification);

        protected abstract Task<bool> DoExistsAsync(ISpecification<TAggregateRoot> specification);

        /// <summary>
        ///     Removes the entity from the repository.
        /// </summary>
        /// <param name="entity">The entity to be removed.</param>
        protected abstract void DoRemove(TAggregateRoot entity);

        /// <summary>
        ///     Updates the entity in the repository.
        /// </summary>
        /// <param name="entity">The entity to be updated.</param>
        protected abstract void DoUpdate(TAggregateRoot entity);

        protected abstract Task<(IQueryable<TAggregateRoot> DataQueryable, long Total)> DoPageFindAsync(int pageIndex,
                                                                                    int pageSize,
                                                                                    ISpecification<TAggregateRoot> specification,
                                                                                    params OrderExpression[] orderExpressions);


        protected abstract (IQueryable<TAggregateRoot> DataQueryable, long Total) DoPageFind(int pageIndex,
                                                                         int pageSize,
                                                                         ISpecification<TAggregateRoot> specification,
                                                                         params OrderExpression[] orderExpressions);

        #endregion

        #region IRepository<TEntity> Members

        /// <summary>
        ///     Adds an entity to the repository.
        /// </summary>
        /// <param name="entities"></param>
        public void Add(IEnumerable<TAggregateRoot> entities)
        {
            DoAdd(entities);
        }

        public void Add(TAggregateRoot entity)
        {
            DoAdd(entity);
        }
        public Task AddAsync(IEnumerable<TAggregateRoot> entities)
        {
            return DoAddAsync(entities);
        }

        public Task AddAsync(TAggregateRoot entity)
        {
            return DoAddAsync(entity);
        }
        /// <summary>
        ///     Gets the entity instance from repository by a given key.
        /// </summary>
        /// <param name="keyValues">The key of the entity.</param>
        /// <returns>The instance of the entity.</returns>
        public TAggregateRoot GetByKey(params object[] keyValues)
        {
            return DoGetByKey(keyValues);
        }

        public Task<TAggregateRoot> GetByKeyAsync(params object[] keyValues)
        {
            return DoGetByKeyAsync(keyValues);
        }

        /// <summary>
        ///     Removes the entity from the repository.
        /// </summary>
        /// <param name="entity">The entity to be removed.</param>
        public void Remove(TAggregateRoot entity)
        {
            DoRemove(entity);
        }

        public void Remove(IEnumerable<TAggregateRoot> entities)
        {
            foreach (var entity in entities)
            {
                DoRemove(entity);
            }
        }

        /// <summary>
        ///     Updates the entity in the repository.
        /// </summary>
        /// <param name="entity">The entity to be updated.</param>
        public void Update(TAggregateRoot entity)
        {
            DoUpdate(entity);
        }

        /// <summary>
        ///     Finds all the aggregate roots from repository, sorting by using the provided sort predicate
        ///     and the specified sort order.
        /// </summary>
        /// <param name="orderExpressions">The <see cref="SortOrder" /> enum which specifies the sort order.</param>
        /// <returns>
        ///     All the aggregate roots got from the repository, with the aggregate roots being sorted by
        ///     using the provided sort predicate and the sort order.
        /// </returns>
        public IQueryable<TAggregateRoot> FindAll(params OrderExpression[] orderExpressions)
        {
            return DoFindAll(orderExpressions);
        }

        /// <summary>
        ///     Finds all the aggregate roots that match the given specification.
        /// </summary>
        /// <param name="specification">The specification with which the aggregate roots should match.</param>
        /// <returns>All the aggregate roots that match the given specification.</returns>
        public IQueryable<TAggregateRoot> FindAll(ISpecification<TAggregateRoot> specification)
        {
            return DoFindAll(specification);
        }

        /// <summary>
        ///     Finds all the aggregate roots that match the given specification, and sorts the aggregate roots
        ///     by using the provided sort predicate and the specified sort order.
        /// </summary>
        /// <param name="specification">The specification with which the aggregate roots should match.</param>
        /// <param name="orderExpressions">The sort predicate which is used for sorting.</param>
        /// <returns>
        ///     All the aggregate roots that match the given specification and were sorted by using the given sort predicate
        ///     and the sort order.
        /// </returns>
        public IQueryable<TAggregateRoot> FindAll(ISpecification<TAggregateRoot> specification,
                                                  params OrderExpression[] orderExpressions)
        {
            return DoFindAll(specification, orderExpressions);
        }

        /// <summary>
        ///     Finds a single aggregate root that matches the given specification.
        /// </summary>
        /// <param name="specification">The specification with which the aggregate root should match.</param>
        /// <returns>The instance of the aggregate root.</returns>
        public TAggregateRoot Find(ISpecification<TAggregateRoot> specification)
        {
            return DoFind(specification);
        }

        public Task<TAggregateRoot> FindAsync(ISpecification<TAggregateRoot> specification)
        {
            return DoFindAsync(specification);
        }

        /// <summary>
        ///     Checkes whether the aggregate root which matches the given specification exists.
        /// </summary>
        /// <param name="specification">The specification with which the aggregate root should match.</param>
        /// <returns>True if the aggregate root exists, otherwise false.</returns>
        public bool Exists(ISpecification<TAggregateRoot> specification)
        {
            return DoExists(specification);
        }

        public Task<bool> ExistsAsync(ISpecification<TAggregateRoot> specification)
        {
            return DoExistsAsync(specification);
        }

        #endregion

        #region IRepository<TAggregateRoot> Members

        public TAggregateRoot Find(Expression<Func<TAggregateRoot, bool>> specification)
        {
            return DoFind(Specification<TAggregateRoot>.Eval(specification));
        }

        public Task<TAggregateRoot> FindAsync(Expression<Func<TAggregateRoot, bool>> specification)
        {
            return DoFindAsync(Specification<TAggregateRoot>.Eval(specification));
        }

        #endregion

        #region IRepository<TAggregateRoot> Members

        public IQueryable<TAggregateRoot> FindAll(Expression<Func<TAggregateRoot, bool>> specification)
        {
            return DoFindAll(Specification<TAggregateRoot>.Eval(specification));
        }

        public IQueryable<TAggregateRoot> FindAll(Expression<Func<TAggregateRoot, bool>> specification,
                                                  params OrderExpression[] orderExpressions)
        {
            return DoFindAll(Specification<TAggregateRoot>.Eval(specification), orderExpressions);
        }

        public bool Exists(Expression<Func<TAggregateRoot, bool>> specification)
        {
            return DoExists(Specification<TAggregateRoot>.Eval(specification));
        }

        public Task<bool> ExistsAsync(Expression<Func<TAggregateRoot, bool>> specification)
        {
            return DoExistsAsync(Specification<TAggregateRoot>.Eval(specification));
        }

        public (IQueryable<TAggregateRoot> DataQueryable, long Total) PageFind(int pageIndex,
                                                           int pageSize,
                                                           Expression<Func<TAggregateRoot, bool>> expression,
                                                           params OrderExpression[] orderExpressions)
        {
            return DoPageFind(pageIndex, pageSize, Specification<TAggregateRoot>.Eval(expression), orderExpressions);
        }

        public Task<(IQueryable<TAggregateRoot> DataQueryable, long Total)> PageFindAsync(int pageIndex,
                                                                           int pageSize,
                                                                           Expression<Func<TAggregateRoot, bool>> specification,
                                                                           params OrderExpression[] orderExpressions)
        {
            return DoPageFindAsync(pageIndex,
                                   pageSize, 
                                   Specification<TAggregateRoot>.Eval(specification),
                                   orderExpressions);
        }

        public (IQueryable<TAggregateRoot> DataQueryable, long Total) PageFind(int pageIndex,
                                                           int pageSize,
                                                           ISpecification<TAggregateRoot> specification,
                                                           params OrderExpression[] orderExpressions)
        {
            return DoPageFind(pageIndex, pageSize, specification, orderExpressions);
        }

        public Task<(IQueryable<TAggregateRoot> DataQueryable, long Total)> PageFindAsync(int pageIndex,
                                                                           int pageSize,
                                                                           ISpecification<TAggregateRoot> specification,
                                                                           params OrderExpression[] orderExpressions)
        {
            return DoPageFindAsync(pageIndex, pageSize, specification, orderExpressions);
        }

        #endregion

        #region IRepository<TAggregateRoot> Members
        public long Count()
        {
            return DoCount();
        }

        public Task<long> CountAsync()
        {
            return DoCountAsync();
        }
        public long Count(Expression<Func<TAggregateRoot, bool>> specification)
        {
            return DoCount(specification);
        }

        public Task<long> CountAsync(Expression<Func<TAggregateRoot, bool>> specification)
        {
            return DoCountAsync(specification);
        }

        public long Count(ISpecification<TAggregateRoot> specification)
        {
            return DoCount(specification);
        }

        public Task<long> CountAsync(ISpecification<TAggregateRoot> specification)
        {
            return DoCountAsync(specification);
        }

        protected abstract long DoCount(ISpecification<TAggregateRoot> specification);
        protected abstract Task<long> DoCountAsync(ISpecification<TAggregateRoot> specification);

        protected abstract long DoCount(Expression<Func<TAggregateRoot, bool>> specification);
        protected abstract Task<long> DoCountAsync(Expression<Func<TAggregateRoot, bool>> specification);

        protected abstract long DoCount();
        protected abstract Task<long> DoCountAsync();

        protected abstract void DoReload(TAggregateRoot entity);
        protected abstract Task DoReloadAsync(TAggregateRoot entity);

        public void Reload(TAggregateRoot entity)
        {
            DoReload(entity);
        }

        public Task ReloadAsync(TAggregateRoot entity)
        {
            return DoReloadAsync(entity);
        }

        #endregion
    }
}