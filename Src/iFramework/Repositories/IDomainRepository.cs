﻿using System;
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
    public interface IDomainRepository
    {
        /// <summary>
        ///     Adds an entity to the repository.
        /// </summary>
        /// <param name="entities"></param>
        void Add<TAggregateRoot>(IEnumerable<TAggregateRoot> entities);

        void Add<TAggregateRoot>(TAggregateRoot entity) ;

        /// <summary>
        ///     Gets the entity instance from repository by a given key.
        /// </summary>
        /// <param name="keyValues"></param>
        /// <returns>The instance of the entity.</returns>
        TAggregateRoot GetByKey<TAggregateRoot>(params object[] keyValues);

        Task<TAggregateRoot> GetByKeyAsync<TAggregateRoot>(params object[] keyValues);


        long Count<TAggregateRoot>();

        Task<long> CountAsync<TAggregateRoot>();

        long Count<TAggregateRoot>(ISpecification<TAggregateRoot> specification);

        Task<long> CountAsync<TAggregateRoot>(ISpecification<TAggregateRoot> specification);

        long Count<TAggregateRoot>(Expression<Func<TAggregateRoot, bool>> specification);

        Task<long> CountAsync<TAggregateRoot>(Expression<Func<TAggregateRoot, bool>> specification);


        void Reload<TEntity>(TEntity entity) where TEntity : IEntity;
        Task ReloadAsync<TEntity>(TEntity entity) where TEntity : IEntity;

        /// <summary>
        ///     Finds all the aggregate roots from repository, sorting by using the provided sort predicate
        ///     and the specified sort order.
        /// </summary>
        /// <param name="orderExpressions"></param>
        /// <returns>
        ///     All the aggregate roots got from the repository, with the aggregate roots being sorted by
        ///     using the provided sort predicate and the sort order.
        /// </returns>
        IQueryable<TAggregateRoot> FindAll<TAggregateRoot>(params OrderExpression[] orderExpressions);

        /// <summary>
        ///     <summary>
        ///         Finds all the aggregate roots that match the given specification, and sorts the aggregate roots
        ///         by using the provided sort predicate and the specified sort order.
        ///     </summary>
        ///     <param name="specification">The specification with which the aggregate roots should match.</param>
        ///     <param name="orderExpressions">The sort predicate which is used for sorting.</param>
        ///     <returns>
        ///         All the aggregate roots that match the given specification and were sorted by using the given sort
        ///         predicate and the sort order.
        ///     </returns>
        /// </summary>
        IQueryable<TAggregateRoot> FindAll<TAggregateRoot>(ISpecification<TAggregateRoot> specification,
                                                           params OrderExpression[] orderExpressions);

        IQueryable<TAggregateRoot> FindAll<TAggregateRoot>(Expression<Func<TAggregateRoot, bool>> specification,
                                                           params OrderExpression[] orderExpressions);

        /// <summary>
        ///     Finds a single aggregate root that matches the given specification.
        /// </summary>
        /// <param name="specification">The specification with which the aggregate root should match.</param>
        /// <returns>The instance of the aggregate root.</returns>
        TAggregateRoot Find<TAggregateRoot>(ISpecification<TAggregateRoot> specification);

        Task<TAggregateRoot> FindAsync<TAggregateRoot>(ISpecification<TAggregateRoot> specification);

        TAggregateRoot Find<TAggregateRoot>(Expression<Func<TAggregateRoot, bool>> specification);

        Task<TAggregateRoot> FindAsync<TAggregateRoot>(Expression<Func<TAggregateRoot, bool>> specification);

        /// <summary>
        ///     Checkes whether the aggregate root which matches the given specification exists.
        /// </summary>
        /// <param name="specification">The specification with which the aggregate root should match.</param>
        /// <returns>True if the aggregate root exists, otherwise false.</returns>
        bool Exists<TAggregateRoot>(ISpecification<TAggregateRoot> specification);

        Task<bool> ExistsAsync<TAggregateRoot>(ISpecification<TAggregateRoot> specification);

        bool Exists<TAggregateRoot>(Expression<Func<TAggregateRoot, bool>> specification);

        Task<bool> ExistsAsync<TAggregateRoot>(Expression<Func<TAggregateRoot, bool>> specification);

        /// <summary>
        ///     Removes the entity from the repository.
        /// </summary>
        /// <param name="entity">The entity to be removed.</param>
        void Remove<TAggregateRoot>(TAggregateRoot entity);

        void Remove<TAggregateRoot>(IEnumerable<TAggregateRoot> entities);

        /// <summary>
        ///     Updates the entity in the repository.
        /// </summary>
        /// <param name="entity">The entity to be updated.</param>
        void Update<TAggregateRoot>(TAggregateRoot entity);

        (IQueryable<TAggregateRoot> DataQueryable, long Total) PageFind<TAggregateRoot>(int pageIndex,
                                                                    int pageSize,
                                                                    Expression<Func<TAggregateRoot, bool>> specification,
                                                                    params OrderExpression[] orderExpressions);


        Task<(IQueryable<TAggregateRoot> DataQueryable, long Total)> PageFindAsync<TAggregateRoot>(int pageIndex,
                                                                               int pageSize,
                                                                               Expression<Func<TAggregateRoot, bool>> specification,
                                                                               params OrderExpression[] orderExpressions);

        (IQueryable<TAggregateRoot> DataQueryable, long Total) PageFind<TAggregateRoot>(int pageIndex,
                                                                    int pageSize,
                                                                    ISpecification<TAggregateRoot> specification,
                                                                    params OrderExpression[] orderExpressions);

        Task<(IQueryable<TAggregateRoot> DataQueryable, long Total)> PageFindAsync<TAggregateRoot>(int pageIndex,
                                                                               int pageSize,
                                                                               ISpecification<TAggregateRoot> specification,
                                                                               params OrderExpression[] orderExpressions);
    }
}