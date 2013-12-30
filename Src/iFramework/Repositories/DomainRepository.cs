using System;
using System.Collections.Generic;
using IFramework.Specifications;
using IFramework.UnitOfWork;
using System.Linq.Expressions;
using System.Linq;
using IFramework.Domain;

namespace IFramework.Repositories
{
    /// <summary>
    /// Represents the base class for repositories.
    /// </summary>
    /// <typeparam name="TAggregateRoot">The type of the aggregate root.</typeparam>
    public class DomainRepository: IDomainRepository
    {
        #region Protected Fields
        protected IUnitOfWork _unitOfWork;
        #endregion

        #region Construct
        /// <summary>
        /// Initializes a new instance of <c>Repository&lt;TAggregateRoot&gt;</c> class.
        /// </summary>
        /// <param name="context">The repository context being used by the repository.</param>
        public DomainRepository(IUnitOfWork unitOfWork)
        {
            this._unitOfWork = unitOfWork;
        }
        #endregion

        #region IRepository Members

        public IUnitOfWork UnitOfWork
        {
            get { return _unitOfWork; }
        }

        public void Add<TAggregateRoot>(IQueryable<TAggregateRoot> entities) where TAggregateRoot : class, IAggregateRoot
        {
            _unitOfWork.GetRepository<TAggregateRoot>().Add(entities);
        }

        public void Add<TAggregateRoot>(TAggregateRoot entity) where TAggregateRoot : class, IAggregateRoot
        {
            _unitOfWork.GetRepository<TAggregateRoot>().Add(entity);
        }

        public TAggregateRoot GetByKey<TAggregateRoot>(params object[] keyValues) where TAggregateRoot : class, IAggregateRoot
        {
            return _unitOfWork.GetRepository<TAggregateRoot>().GetByKey(keyValues);
        }

        public long Count<TAggregateRoot>(ISpecification<TAggregateRoot> specification) where TAggregateRoot : class, IAggregateRoot
        {
            return _unitOfWork.GetRepository<TAggregateRoot>().Count(specification);
        }

        public long Count<TAggregateRoot>(Expression<Func<TAggregateRoot, bool>> specification) where TAggregateRoot : class, IAggregateRoot
        {
            return _unitOfWork.GetRepository<TAggregateRoot>().Count(specification);
        }

        public IQueryable<TAggregateRoot> FindAll<TAggregateRoot>(params OrderExpression[] orderExpressions) where TAggregateRoot : class, IAggregateRoot
        {
            return _unitOfWork.GetRepository<TAggregateRoot>().FindAll(orderExpressions);
        }

        public IQueryable<TAggregateRoot> FindAll<TAggregateRoot>(ISpecification<TAggregateRoot> specification, params OrderExpression[] orderExpressions) where TAggregateRoot : class, IAggregateRoot
        {
            return _unitOfWork.GetRepository<TAggregateRoot>().FindAll(specification, orderExpressions);
        }

        public IQueryable<TAggregateRoot> FindAll<TAggregateRoot>(Expression<Func<TAggregateRoot, bool>> specification, params OrderExpression[] orderExpressions) where TAggregateRoot : class, IAggregateRoot
        {
            return _unitOfWork.GetRepository<TAggregateRoot>().FindAll(specification, orderExpressions);
        }

        public TAggregateRoot Find<TAggregateRoot>(ISpecification<TAggregateRoot> specification) where TAggregateRoot : class, IAggregateRoot
        {
            return _unitOfWork.GetRepository<TAggregateRoot>().Find(specification);
        }

        public TAggregateRoot Find<TAggregateRoot>(Expression<Func<TAggregateRoot, bool>> specification) where TAggregateRoot : class, IAggregateRoot
        {
            return _unitOfWork.GetRepository<TAggregateRoot>().Find(specification);
        }

        public bool Exists<TAggregateRoot>(ISpecification<TAggregateRoot> specification) where TAggregateRoot : class, IAggregateRoot
        {
            return _unitOfWork.GetRepository<TAggregateRoot>().Exists(specification);
        }

        public bool Exists<TAggregateRoot>(Expression<Func<TAggregateRoot, bool>> specification) where TAggregateRoot : class, IAggregateRoot
        {
            return _unitOfWork.GetRepository<TAggregateRoot>().Exists(specification);
        }

        public void Remove<TAggregateRoot>(TAggregateRoot entity) where TAggregateRoot : class, IAggregateRoot
        {
            _unitOfWork.GetRepository<TAggregateRoot>().Remove(entity);
        }

        public void Remove<TAggregateRoot>(IEnumerable<TAggregateRoot> entities) where TAggregateRoot : class, IAggregateRoot
        {
            _unitOfWork.GetRepository<TAggregateRoot>().Remove(entities);
        }

        public void Update<TAggregateRoot>(TAggregateRoot entity) where TAggregateRoot : class, IAggregateRoot
        {
            _unitOfWork.GetRepository<TAggregateRoot>().Update(entity);
        }

        public IQueryable<TAggregateRoot> PageFind<TAggregateRoot>(int pageIndex, int pageSize, Expression<Func<TAggregateRoot, bool>> specification, params OrderExpression[] orderExpressions) where TAggregateRoot : class, IAggregateRoot
        {
            return _unitOfWork.GetRepository<TAggregateRoot>().PageFind(pageIndex, pageSize, specification, orderExpressions);
        }

        public IQueryable<TAggregateRoot> PageFind<TAggregateRoot>(int pageIndex, int pageSize, Expression<Func<TAggregateRoot, bool>> specification, ref long totalCount, params OrderExpression[] orderExpressions) where TAggregateRoot : class, IAggregateRoot
        {
            return _unitOfWork.GetRepository<TAggregateRoot>().PageFind(pageIndex, pageSize, specification, ref totalCount, orderExpressions);
        }

        public IQueryable<TAggregateRoot> PageFind<TAggregateRoot>(int pageIndex, int pageSize, ISpecification<TAggregateRoot> specification, ref long totalCount, params OrderExpression[] orderExpressions) where TAggregateRoot : class, IAggregateRoot
        {
            return _unitOfWork.GetRepository<TAggregateRoot>().PageFind(pageIndex, pageSize, specification, ref totalCount, orderExpressions);
        }

        public IQueryable<TAggregateRoot> PageFind<TAggregateRoot>(int pageIndex, int pageSize, ISpecification<TAggregateRoot> specification, params OrderExpression[] orderExpressions) where TAggregateRoot : class, IAggregateRoot
        {
            return _unitOfWork.GetRepository<TAggregateRoot>().PageFind(pageIndex, pageSize, specification, orderExpressions);
        }

        #endregion
    }
}
