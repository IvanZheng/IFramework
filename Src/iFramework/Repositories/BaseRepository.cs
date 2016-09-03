using System;
using System.Collections.Generic;
using IFramework.Specifications;
using IFramework.UnitOfWork;
using System.Linq.Expressions;
using System.Linq;
using IFramework.Domain;
using System.Threading.Tasks;

namespace IFramework.Repositories
{
    /// <summary>
    /// Represents the base class for repositories.
    /// </summary>
    /// <typeparam name="TAggregateRoot">The type of the aggregate root.</typeparam>
    public abstract class BaseRepository<TAggregateRoot> : IRepository<TAggregateRoot>
         where TAggregateRoot : class
    {
        #region Protected Methods
        /// <summary>
        /// Adds an entity to the repository.
        /// </summary>
        /// <param name="entity">The entity object to be added.</param>
        protected abstract void DoAdd(IQueryable<TAggregateRoot> entities);
        protected abstract void DoAdd(TAggregateRoot entity);
        /// <summary>
        /// Gets the entity instance from repository by a given key.
        /// </summary>
        /// <param name="key">The key of the entity.</param>
        /// <returns>The instance of the entity.</returns>
        protected abstract TAggregateRoot DoGetByKey(params object[] keyValues);
        protected abstract Task<TAggregateRoot> DoGetByKeyAsync(params object[] keyValues);


        /// <summary>
        /// Finds all the aggregate roots from repository, sorting by using the provided sort predicate
        /// and the specified sort order.
        /// </summary>
        /// <param name="sortPredicate">The sort predicate which is used for sorting.</param>
        /// <param name="sortOrder">The <see cref="Framework.Enumerations.SortOrder"/> enum which specifies the sort order.</param>
        /// <returns>All the aggregate roots got from the repository, with the aggregate roots being sorted by
        /// using the provided sort predicate and the sort order.</returns>
        protected virtual IQueryable<TAggregateRoot> DoFindAll(params OrderExpression[] orderExpressions)
        {
            return DoFindAll(new AllSpecification<TAggregateRoot>(), orderExpressions);
        }

        /// <summary>
        /// Finds all the aggregate roots that match the given specification, and sorts the aggregate roots
        /// by using the provided sort predicate and the specified sort order.
        /// </summary>
        /// <param name="specification">The specification with which the aggregate roots should match.</param>
        /// <param name="sortPredicate">The sort predicate which is used for sorting.</param>
        /// <param name="sortOrder">The <see cref="Framework.Enumerations.SortOrder"/> enum which specifies the sort order.</param>
        /// <returns>All the aggregate roots that match the given specification and were sorted by using the given sort predicate and the sort order.</returns>
        protected abstract IQueryable<TAggregateRoot> DoFindAll(ISpecification<TAggregateRoot> specification, params OrderExpression[] orderExpressions);
        /// <summary>
        /// Finds a single aggregate root that matches the given specification.
        /// </summary>
        /// <param name="specification">The specification with which the aggregate root should match.</param>
        /// <returns>The instance of the aggregate root.</returns>
        protected abstract TAggregateRoot DoFind(ISpecification<TAggregateRoot> specification);
        protected abstract Task<TAggregateRoot> DoFindAsync(ISpecification<TAggregateRoot> specification);


        /// <summary>
        /// Checkes whether the aggregate root which matches the given specification exists.
        /// </summary>
        /// <param name="specification">The specification with which the aggregate root should match.</param>
        /// <returns>True if the aggregate root exists, otherwise false.</returns>
        protected abstract bool DoExists(ISpecification<TAggregateRoot> specification);
        protected abstract Task<bool> DoExistsAsync(ISpecification<TAggregateRoot> specification);

        /// <summary>
        /// Removes the entity from the repository.
        /// </summary>
        /// <param name="entity">The entity to be removed.</param>
        protected abstract void DoRemove(TAggregateRoot entity);
        /// <summary>
        /// Updates the entity in the repository.
        /// </summary>
        /// <param name="entity">The entity to be updated.</param>
        protected abstract void DoUpdate(TAggregateRoot entity);

        protected abstract IQueryable<TAggregateRoot> DoPageFind(int pageIndex, int pageSize, ISpecification<TAggregateRoot> specification, ref long totalCount, params OrderExpression[] orderExpressions);

        protected abstract Task<Tuple<IQueryable<TAggregateRoot>, long>> DoPageFindAsync(int pageIndex, int pageSize, ISpecification<TAggregateRoot> specification, params OrderExpression[] orderExpressions);

        protected abstract IQueryable<TAggregateRoot> DoPageFind(int pageIndex, int pageSize, ISpecification<TAggregateRoot> specification, params OrderExpression[] orderExpressions);

        #endregion

        #region IRepository<TEntity> Members

        /// <summary>
        /// Adds an entity to the repository.
        /// </summary>
        /// <param name="entity">The entity object to be added.</param>
        /// <exception cref="Framework.Repositories.RepositoryException">Occurs when failed to perform the specific operation.</exception>
        public void Add(IQueryable<TAggregateRoot> entities)
        {
            this.DoAdd(entities);
        }

        public void Add(TAggregateRoot entity)
        {
            this.DoAdd(entity);
        }
        /// <summary>
        /// Gets the entity instance from repository by a given key.
        /// </summary>
        /// <param name="key">The key of the entity.</param>
        /// <returns>The instance of the entity.</returns>
        /// <exception cref="Framework.Repositories.RepositoryException">Occurs when failed to perform the specific operation.</exception>
        public TAggregateRoot GetByKey(params object[] keyValues)
        {
            return this.DoGetByKey(keyValues);
        }
        public async Task<TAggregateRoot> GetByKeyAsync(params object[] keyValues)
        {
            return await this.DoGetByKeyAsync(keyValues).ConfigureAwait(false);
        }
        /// <summary>
        /// Removes the entity from the repository.
        /// </summary>
        /// <param name="entity">The entity to be removed.</param>
        /// <exception cref="Framework.Repositories.RepositoryException">Occurs when failed to perform the specific operation.</exception>
        public void Remove(TAggregateRoot entity)
        {
            this.DoRemove(entity);
        }

        public void Remove(IEnumerable<TAggregateRoot> entities)
        {
            foreach (var entity in entities)
            {
                this.DoRemove(entity);
            }
        }
        /// <summary>
        /// Updates the entity in the repository.
        /// </summary>
        /// <param name="entity">The entity to be updated.</param>
        /// <exception cref="Framework.Repositories.RepositoryException">Occurs when failed to perform the specific operation.</exception>
        public void Update(TAggregateRoot entity)
        {
            this.DoUpdate(entity);
        }

        /// <summary>
        /// Finds all the aggregate roots from repository, sorting by using the provided sort predicate
        /// and the specified sort order.
        /// </summary>
        /// <param name="sortPredicate">The sort predicate which is used for sorting.</param>
        /// <param name="sortOrder">The <see cref="Framework.Enumerations.SortOrder"/> enum which specifies the sort order.</param>
        /// <returns>All the aggregate roots got from the repository, with the aggregate roots being sorted by
        /// using the provided sort predicate and the sort order.</returns>
        /// <exception cref="Framework.Repositories.RepositoryException">Occurs when failed to perform the specific operation.</exception>
        public IQueryable<TAggregateRoot> FindAll(params OrderExpression[] orderExpressions)
        {
            return this.DoFindAll(orderExpressions);
        }

        /// <summary>
        /// Finds all the aggregate roots that match the given specification.
        /// </summary>
        /// <param name="specification">The specification with which the aggregate roots should match.</param>
        /// <returns>All the aggregate roots that match the given specification.</returns>
        /// <exception cref="Framework.Repositories.RepositoryException">Occurs when failed to perform the specific operation.</exception>


        public IQueryable<TAggregateRoot> FindAll(ISpecification<TAggregateRoot> specification)
        {
            return this.DoFindAll(specification);
        }
        /// <summary>
        /// Finds all the aggregate roots that match the given specification, and sorts the aggregate roots
        /// by using the provided sort predicate and the specified sort order.
        /// </summary>
        /// <param name="specification">The specification with which the aggregate roots should match.</param>
        /// <param name="sortPredicate">The sort predicate which is used for sorting.</param>
        /// <param name="sortOrder">The <see cref="Framework.Enumerations.SortOrder"/> enum which specifies the sort order.</param>
        /// <returns>All the aggregate roots that match the given specification and were sorted by using the given sort predicate and the sort order.</returns>
        /// <exception cref="Framework.Repositories.RepositoryException">Occurs when failed to perform the specific operation.</exception>

        public IQueryable<TAggregateRoot> FindAll(ISpecification<TAggregateRoot> specification, params OrderExpression[] orderExpressions)
        {
            return this.DoFindAll(specification, orderExpressions);
        }
        /// <summary>
        /// Finds a single aggregate root that matches the given specification.
        /// </summary>
        /// <param name="specification">The specification with which the aggregate root should match.</param>
        /// <returns>The instance of the aggregate root.</returns>
        /// <exception cref="Framework.Repositories.RepositoryException">Occurs when failed to perform the specific operation.</exception>
        public TAggregateRoot Find(ISpecification<TAggregateRoot> specification)
        {
            return this.DoFind(specification);
        }
        public async Task<TAggregateRoot> FindAsync(ISpecification<TAggregateRoot> specification)
        {
            return await this.DoFindAsync(specification).ConfigureAwait(false);
        }
        /// <summary>
        /// Checkes whether the aggregate root which matches the given specification exists.
        /// </summary>
        /// <param name="specification">The specification with which the aggregate root should match.</param>
        /// <returns>True if the aggregate root exists, otherwise false.</returns>
        /// <exception cref="Framework.Repositories.RepositoryException">Occurs when failed to perform the specific operation.</exception>
        public bool Exists(ISpecification<TAggregateRoot> specification)
        {
            return this.DoExists(specification);
        }
        public async Task<bool> ExistsAsync(ISpecification<TAggregateRoot> specification)
        {
            return await this.DoExistsAsync(specification).ConfigureAwait(false);
        }
        #endregion

        #region IRepository<TAggregateRoot> Members
        public TAggregateRoot Find(System.Linq.Expressions.Expression<Func<TAggregateRoot, bool>> specification)
        {
            return DoFind(Specification<TAggregateRoot>.Eval(specification));
        }

        public async Task<TAggregateRoot> FindAsync(System.Linq.Expressions.Expression<Func<TAggregateRoot, bool>> specification)
        {
            return await DoFindAsync(Specification<TAggregateRoot>.Eval(specification)).ConfigureAwait(false);
        }
        #endregion

        #region IRepository<TAggregateRoot> Members


        public IQueryable<TAggregateRoot> FindAll(System.Linq.Expressions.Expression<Func<TAggregateRoot, bool>> specification)
        {
            return DoFindAll(Specification<TAggregateRoot>.Eval(specification));
        }

        public IQueryable<TAggregateRoot> FindAll(System.Linq.Expressions.Expression<Func<TAggregateRoot, bool>> specification, params OrderExpression[] orderExpressions)
        {
            return DoFindAll(Specification<TAggregateRoot>.Eval(specification), orderExpressions);
        }
        public bool Exists(System.Linq.Expressions.Expression<Func<TAggregateRoot, bool>> specification)
        {
            return DoExists(Specification<TAggregateRoot>.Eval(specification));
        }

        public async Task<bool> ExistsAsync(System.Linq.Expressions.Expression<Func<TAggregateRoot, bool>> specification)
        {
            return await DoExistsAsync(Specification<TAggregateRoot>.Eval(specification)).ConfigureAwait(false);
        }

        public IQueryable<TAggregateRoot> PageFind(int pageIndex, int pageSize, System.Linq.Expressions.Expression<Func<TAggregateRoot, bool>> specification, params OrderExpression[] orderExpressions)
        {
            return DoPageFind(pageIndex, pageSize, Specification<TAggregateRoot>.Eval(specification), orderExpressions);
        }

        public IQueryable<TAggregateRoot> PageFind(int pageIndex, int pageSize, System.Linq.Expressions.Expression<Func<TAggregateRoot, bool>> specification, ref long totalCount, params OrderExpression[] orderExpressions)
        {
            return DoPageFind(pageIndex, pageSize, Specification<TAggregateRoot>.Eval(specification), ref totalCount, orderExpressions);

        }

        public async Task<Tuple<IQueryable<TAggregateRoot>, long>> PageFindAsync(int pageIndex, int pageSize, System.Linq.Expressions.Expression<Func<TAggregateRoot, bool>> specification, params OrderExpression[] orderExpressions)
        {
            return await DoPageFindAsync(pageIndex, pageSize, Specification<TAggregateRoot>.Eval(specification), orderExpressions)
                                       .ConfigureAwait(false);

        }

        public IQueryable<TAggregateRoot> PageFind(int pageIndex, int pageSize, ISpecification<TAggregateRoot> specification, ref long totalCount, params OrderExpression[] orderExpressions)
        {
            return DoPageFind(pageIndex, pageSize, specification, ref totalCount, orderExpressions);
        }

        public async Task<Tuple<IQueryable<TAggregateRoot>, long>> PageFindAsync(int pageIndex, int pageSize, ISpecification<TAggregateRoot> specification, params OrderExpression[] orderExpressions)
        {
            return await DoPageFindAsync(pageIndex, pageSize, specification, orderExpressions).ConfigureAwait(false);
        }

        public IQueryable<TAggregateRoot> PageFind(int pageIndex, int pageSize, ISpecification<TAggregateRoot> specification, params OrderExpression[] orderExpressions)
        {
            return DoPageFind(pageIndex, pageSize, specification, orderExpressions);
        }


        #endregion

        #region IRepository<TAggregateRoot> Members

        public long Count(Expression<Func<TAggregateRoot, bool>> specification)
        {
            return DoCount(specification);
        }

        public async Task<long> CountAsync(Expression<Func<TAggregateRoot, bool>> specification)
        {
            return await DoCountAsync(specification).ConfigureAwait(false);
        }

        public long Count(ISpecification<TAggregateRoot> specification)
        {
            return DoCount(specification);
        }
        public async Task<long> CountAsync(ISpecification<TAggregateRoot> specification)
        {
            return await DoCountAsync(specification).ConfigureAwait(false);
        }

        protected abstract long DoCount(ISpecification<TAggregateRoot> specification);
        protected abstract Task<long> DoCountAsync(ISpecification<TAggregateRoot> specification);

        protected abstract long DoCount(Expression<Func<TAggregateRoot, bool>> specification);
        protected abstract Task<long> DoCountAsync(Expression<Func<TAggregateRoot, bool>> specification);

        #endregion
    }
}
 