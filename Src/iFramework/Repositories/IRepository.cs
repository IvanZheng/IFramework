using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using IFramework.Specifications;

namespace IFramework.Repositories
{
    public interface IRepository { }

    /// <summary>
    ///     Represents the repositories.
    /// </summary>
    /// <typeparam name="TAggregateRoot">The type of the aggregation root with which the repository is working.</typeparam>
    public interface IRepository<TAggregateRoot> : IRepository
        where TAggregateRoot : class
    {
        /// <summary>
        ///     Adds an entity to the repository.
        /// </summary>
        /// <param name="entities">The entity object to be added.</param>
        void Add(IEnumerable<TAggregateRoot> entities);

        /// <summary>
        ///     Adds an entity to the repository.
        /// </summary>
        /// <param name="entity">The entity object to be added.</param>
        void Add(TAggregateRoot entity);

        /// <summary>
        ///     <para>
        ///         Begins tracking the given entity, and any other reachable entities that are
        ///         not already being tracked, in the <see cref="F:Microsoft.EntityFrameworkCore.EntityState.Added" /> state such that they will
        ///         be inserted into the database when <see cref="M:Microsoft.EntityFrameworkCore.DbContext.SaveChanges" /> is called.
        ///     </para>
        ///     <para>
        ///         This method is async only to allow special value generators, such as the one used by
        ///         'Microsoft.EntityFrameworkCore.Metadata.SqlServerValueGenerationStrategy.SequenceHiLo',
        ///         to access the database asynchronously. For all other cases the non async method should be used.
        ///     </para>
        ///     <para>
        ///         Use <see cref="P:Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry.State" /> to set the state of only a single entity.
        ///     </para>
        /// </summary>
        /// <param name="entities"> The entities to add. </param>
        /// <returns>
        ///     A task that represents the asynchronous Add operation. The task result contains the
        ///     <see cref="T:Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry`1" /> for the entity. The entry provides access to change tracking
        ///     information and operations for the entity.
        /// </returns>
        Task AddAsync(IEnumerable<TAggregateRoot> entities);

        /// <summary>
        ///     <para>
        ///         Begins tracking the given entity, and any other reachable entities that are
        ///         not already being tracked, in the <see cref="F:Microsoft.EntityFrameworkCore.EntityState.Added" /> state such that they will
        ///         be inserted into the database when <see cref="M:Microsoft.EntityFrameworkCore.DbContext.SaveChanges" /> is called.
        ///     </para>
        ///     <para>
        ///         This method is async only to allow special value generators, such as the one used by
        ///         'Microsoft.EntityFrameworkCore.Metadata.SqlServerValueGenerationStrategy.SequenceHiLo',
        ///         to access the database asynchronously. For all other cases the non async method should be used.
        ///     </para>
        ///     <para>
        ///         Use <see cref="P:Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry.State" /> to set the state of only a single entity.
        ///     </para>
        /// </summary>
        /// <param name="entity"> The entity to add. </param>
        /// <returns>
        ///     A task that represents the asynchronous Add operation. The task result contains the
        ///     <see cref="T:Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry`1" /> for the entity. The entry provides access to change tracking
        ///     information and operations for the entity.
        /// </returns>
        Task AddAsync(TAggregateRoot entity);
        /// <summary>
        ///     Gets the entity instance from repository by a given key.
        /// </summary>
        /// <param name="keyValues">The key of the entity.</param>
        /// <returns>The instance of the entity.</returns>
        TAggregateRoot GetByKey(params object[] keyValues);

        Task<TAggregateRoot> GetByKeyAsync(params object[] keyValues);


        long Count(ISpecification<TAggregateRoot> specification);
        Task<long> CountAsync(ISpecification<TAggregateRoot> specification);

        long Count(Expression<Func<TAggregateRoot, bool>> specification);
        Task<long> CountAsync(Expression<Func<TAggregateRoot, bool>> specification);

        long Count();
        Task<long> CountAsync();
        /// <summary>
        ///     Finds all the aggregate roots from repository, sorting by using the provided sort predicate
        ///     and the specified sort order.
        /// </summary>
        /// <returns>
        ///     All the aggregate roots got from the repository, with the aggregate roots being sorted by
        ///     using the provided sort predicate and the sort order.
        /// </returns>
        IQueryable<TAggregateRoot> FindAll(params OrderExpression[] orderByExpressions);

        /// <summary>
        ///     Finds all the aggregate roots that match the given specification, and sorts the aggregate roots
        ///     by using the provided sort predicate and the specified sort order.
        /// </summary>
        /// <param name="specification">The specification with which the aggregate roots should match.</param>
        /// <param name="orderByExpressions"></param>
        /// <returns>
        ///     All the aggregate roots that match the given specification and were sorted by using the given sort predicate
        ///     and the sort order.
        /// </returns>
        IQueryable<TAggregateRoot> FindAll(ISpecification<TAggregateRoot> specification,
                                           params OrderExpression[] orderByExpressions);

        IQueryable<TAggregateRoot> FindAll(Expression<Func<TAggregateRoot, bool>> specification,
                                           params OrderExpression[] orderByExpressions);

        /// <summary>
        ///     Finds a single aggregate root that matches the given specification.
        /// </summary>
        /// <param name="specification">The specification with which the aggregate root should match.</param>
        /// <returns>The instance of the aggregate root.</returns>
        TAggregateRoot Find(ISpecification<TAggregateRoot> specification);

        Task<TAggregateRoot> FindAsync(ISpecification<TAggregateRoot> specification);

        TAggregateRoot Find(Expression<Func<TAggregateRoot, bool>> specification);
        Task<TAggregateRoot> FindAsync(Expression<Func<TAggregateRoot, bool>> specification);

        /// <summary>
        ///     Checkes whether the aggregate root which matches the given specification exists.
        /// </summary>
        /// <param name="specification">The specification with which the aggregate root should match.</param>
        /// <returns>True if the aggregate root exists, otherwise false.</returns>
        bool Exists(ISpecification<TAggregateRoot> specification);

        Task<bool> ExistsAsync(ISpecification<TAggregateRoot> specification);

        bool Exists(Expression<Func<TAggregateRoot, bool>> specification);
        Task<bool> ExistsAsync(Expression<Func<TAggregateRoot, bool>> specification);

        /// <summary>
        ///     Removes the entity from the repository.
        /// </summary>
        /// <param name="entity">The entity to be removed.</param>
        void Remove(TAggregateRoot entity);

        void Remove(IEnumerable<TAggregateRoot> entities);

        void Reload(TAggregateRoot entity);
        Task ReloadAsync(TAggregateRoot entity);

        /// <summary>
        ///     Updates the entity in the repository.
        /// </summary>
        /// <param name="entity">The entity to be updated.</param>
        void Update(TAggregateRoot entity);

        (IQueryable<TAggregateRoot> data, long total) PageFind(int pageIndex,
                                                    int pageSize,
                                                    Expression<Func<TAggregateRoot, bool>> expression,
                                                    params OrderExpression[] orderByExpressions);

        Task<(IQueryable<TAggregateRoot> data, long total)> PageFindAsync(int pageIndex,
                                                               int pageSize,
                                                               Expression<Func<TAggregateRoot, bool>> specification,
                                                               params OrderExpression[] orderByExpressions);

        (IQueryable<TAggregateRoot> data, long total) PageFind(int pageIndex,
                                                    int pageSize,
                                                    ISpecification<TAggregateRoot> specification,
                                                    params OrderExpression[] orderByExpressions);


        Task<(IQueryable<TAggregateRoot> data, long total)> PageFindAsync(int pageIndex,
                                                               int pageSize,
                                                               ISpecification<TAggregateRoot> specification,
                                                               params OrderExpression[] orderByExpressions);
    }
}