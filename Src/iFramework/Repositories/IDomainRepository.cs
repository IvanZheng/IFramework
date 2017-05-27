using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using IFramework.Domain;
using IFramework.Specifications;

namespace IFramework.Repositories
{
    /// <summary>
    ///     Represents the repositories.
    /// </summary>
    /// <typeparam name="TAggregateRoot">The type of the aggregation root with which the repository is working.</typeparam>
    public interface IDomainRepository
    {
        /// <summary>
        ///     Adds an entity to the repository.
        /// </summary>
        /// <param name="entity">The entity object to be added.</param>
        void Add<TAggregateRoot>(IEnumerable<TAggregateRoot> entities) where TAggregateRoot : class, IAggregateRoot;

        void Add<TAggregateRoot>(TAggregateRoot entity) where TAggregateRoot : class, IAggregateRoot;

        /// <summary>
        ///     Gets the entity instance from repository by a given key.
        /// </summary>
        /// <param name="key">The key of the entity.</param>
        /// <returns>The instance of the entity.</returns>
        TAggregateRoot GetByKey<TAggregateRoot>(params object[] keyValues) where TAggregateRoot : class, IAggregateRoot;

        Task<TAggregateRoot> GetByKeyAsync<TAggregateRoot>(params object[] keyValues)
            where TAggregateRoot : class, IAggregateRoot;


        long Count<TAggregateRoot>(ISpecification<TAggregateRoot> specification)
            where TAggregateRoot : class, IAggregateRoot;

        Task<long> CountAsync<TAggregateRoot>(ISpecification<TAggregateRoot> specification)
            where TAggregateRoot : class, IAggregateRoot;

        long Count<TAggregateRoot>(Expression<Func<TAggregateRoot, bool>> specification)
            where TAggregateRoot : class, IAggregateRoot;

        Task<long> CountAsync<TAggregateRoot>(Expression<Func<TAggregateRoot, bool>> specification)
            where TAggregateRoot : class, IAggregateRoot;


        /// <summary>
        ///     Finds all the aggregate roots from repository, sorting by using the provided sort predicate
        ///     and the specified sort order.
        /// </summary>
        /// <param name="sortPredicate">The sort predicate which is used for sorting.</param>
        /// <param name="sortOrder">The <see cref="Framework.Enumerations.SortOrder" /> enum which specifies the sort order.</param>
        /// <returns>
        ///     All the aggregate roots got from the repository, with the aggregate roots being sorted by
        ///     using the provided sort predicate and the sort order.
        /// </returns>
        IQueryable<TAggregateRoot> FindAll<TAggregateRoot>(params OrderExpression[] orderExpressions)
            where TAggregateRoot : class, IAggregateRoot;

        /// <summary>
        ///     <summary>
        ///         Finds all the aggregate roots that match the given specification, and sorts the aggregate roots
        ///         by using the provided sort predicate and the specified sort order.
        ///     </summary>
        ///     <param name="specification">The specification with which the aggregate roots should match.</param>
        ///     <param name="sortPredicate">The sort predicate which is used for sorting.</param>
        ///     <param name="sortOrder">The <see cref="Framework.Enumerations.SortOrder" /> enum which specifies the sort order.</param>
        ///     <returns>
        ///         All the aggregate roots that match the given specification and were sorted by using the given sort
        ///         predicate and the sort order.
        ///     </returns>
        IQueryable<TAggregateRoot> FindAll<TAggregateRoot>(ISpecification<TAggregateRoot> specification,
            params OrderExpression[] orderExpressions) where TAggregateRoot : class, IAggregateRoot;

        IQueryable<TAggregateRoot> FindAll<TAggregateRoot>(Expression<Func<TAggregateRoot, bool>> specification,
            params OrderExpression[] orderExpressions) where TAggregateRoot : class, IAggregateRoot;

        /// <summary>
        ///     Finds a single aggregate root that matches the given specification.
        /// </summary>
        /// <param name="specification">The specification with which the aggregate root should match.</param>
        /// <returns>The instance of the aggregate root.</returns>
        TAggregateRoot Find<TAggregateRoot>(ISpecification<TAggregateRoot> specification)
            where TAggregateRoot : class, IAggregateRoot;

        Task<TAggregateRoot> FindAsync<TAggregateRoot>(ISpecification<TAggregateRoot> specification)
            where TAggregateRoot : class, IAggregateRoot;

        TAggregateRoot Find<TAggregateRoot>(Expression<Func<TAggregateRoot, bool>> specification)
            where TAggregateRoot : class, IAggregateRoot;

        Task<TAggregateRoot> FindAsync<TAggregateRoot>(Expression<Func<TAggregateRoot, bool>> specification)
            where TAggregateRoot : class, IAggregateRoot;

        /// <summary>
        ///     Checkes whether the aggregate root which matches the given specification exists.
        /// </summary>
        /// <param name="specification">The specification with which the aggregate root should match.</param>
        /// <returns>True if the aggregate root exists, otherwise false.</returns>
        bool Exists<TAggregateRoot>(ISpecification<TAggregateRoot> specification)
            where TAggregateRoot : class, IAggregateRoot;

        Task<bool> ExistsAsync<TAggregateRoot>(ISpecification<TAggregateRoot> specification)
            where TAggregateRoot : class, IAggregateRoot;

        bool Exists<TAggregateRoot>(Expression<Func<TAggregateRoot, bool>> specification)
            where TAggregateRoot : class, IAggregateRoot;

        Task<bool> ExistsAsync<TAggregateRoot>(Expression<Func<TAggregateRoot, bool>> specification)
            where TAggregateRoot : class, IAggregateRoot;

        /// <summary>
        ///     Removes the entity from the repository.
        /// </summary>
        /// <param name="entity">The entity to be removed.</param>
        void Remove<TAggregateRoot>(TAggregateRoot entity) where TAggregateRoot : class, IAggregateRoot;

        void Remove<TAggregateRoot>(IEnumerable<TAggregateRoot> entities) where TAggregateRoot : class, IAggregateRoot;

        /// <summary>
        ///     Updates the entity in the repository.
        /// </summary>
        /// <param name="entity">The entity to be updated.</param>
        void Update<TAggregateRoot>(TAggregateRoot entity) where TAggregateRoot : class, IAggregateRoot;

        IQueryable<TAggregateRoot> PageFind<TAggregateRoot>(int pageIndex, int pageSize,
            Expression<Func<TAggregateRoot, bool>> specification, params OrderExpression[] orderExpressions)
            where TAggregateRoot : class, IAggregateRoot;

        IQueryable<TAggregateRoot> PageFind<TAggregateRoot>(int pageIndex, int pageSize,
            Expression<Func<TAggregateRoot, bool>> specification, ref long totalCount,
            params OrderExpression[] orderExpressions) where TAggregateRoot : class, IAggregateRoot;

        Task<Tuple<IQueryable<TAggregateRoot>, long>> PageFindAsync<TAggregateRoot>(int pageIndex, int pageSize,
            Expression<Func<TAggregateRoot, bool>> specification, params OrderExpression[] orderExpressions)
            where TAggregateRoot : class, IAggregateRoot;

        IQueryable<TAggregateRoot> PageFind<TAggregateRoot>(int pageIndex, int pageSize,
            ISpecification<TAggregateRoot> specification, ref long totalCount,
            params OrderExpression[] orderExpressions) where TAggregateRoot : class, IAggregateRoot;

        Task<Tuple<IQueryable<TAggregateRoot>, long>> PageFindAsync<TAggregateRoot>(int pageIndex, int pageSize,
            ISpecification<TAggregateRoot> specification, params OrderExpression[] orderExpressions)
            where TAggregateRoot : class, IAggregateRoot;

        IQueryable<TAggregateRoot> PageFind<TAggregateRoot>(int pageIndex, int pageSize,
            ISpecification<TAggregateRoot> specification, params OrderExpression[] orderExpressions)
            where TAggregateRoot : class, IAggregateRoot;
    }
}