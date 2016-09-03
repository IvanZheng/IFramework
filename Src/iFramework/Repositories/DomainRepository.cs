using IFramework.Domain;
using IFramework.IoC;
using IFramework.Specifications;
using IFramework.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.Repositories
{
    public class DomainRepository : IDomainRepository
    {
        object _DbContext;
        IUnitOfWork _UnitOfWork;
        Dictionary<Type, IRepository> _Repositories;
        IContainer _container;
        #region Construct
        /// <summary>
        /// Initializes a new instance of DomainRepository.
        /// </summary>
        /// <param name="context">The repository context being used by the repository.</param>
        public DomainRepository(object dbContext, IUnitOfWork unitOfWork, IContainer container)
        {
            _DbContext = dbContext;
            _UnitOfWork = unitOfWork;
            _container = container;
            _Repositories = new Dictionary<Type, IRepository>();
        }
        #endregion


        public IRepository<TAggregateRoot> GetRepository<TAggregateRoot>()
            where TAggregateRoot : class
        {
            IRepository repository;
            if (!_Repositories.TryGetValue(typeof(IRepository<TAggregateRoot>), out repository))
            {
                repository = _container.Resolve<IRepository<TAggregateRoot>>(new Parameter("dbContext", _DbContext),
                                                                             new Parameter("unitOfWork", _UnitOfWork));
                _Repositories.Add(typeof(IRepository<TAggregateRoot>), repository);
            }
            return repository as IRepository<TAggregateRoot>;
        }


        #region IRepository Members


        public void Add<TAggregateRoot>(IQueryable<TAggregateRoot> entities) where TAggregateRoot : class, IAggregateRoot
        {
            GetRepository<TAggregateRoot>().Add(entities);
        }

        public void Add<TAggregateRoot>(TAggregateRoot entity) where TAggregateRoot : class, IAggregateRoot
        {
            GetRepository<TAggregateRoot>().Add(entity);
        }

        public TAggregateRoot GetByKey<TAggregateRoot>(params object[] keyValues) where TAggregateRoot : class, IAggregateRoot
        {
            return GetRepository<TAggregateRoot>().GetByKey(keyValues);
        }

        public async Task<TAggregateRoot> GetByKeyAsync<TAggregateRoot>(params object[] keyValues) where TAggregateRoot : class, IAggregateRoot
        {
            return await GetRepository<TAggregateRoot>().GetByKeyAsync(keyValues).ConfigureAwait(false);
        }

        public long Count<TAggregateRoot>(ISpecification<TAggregateRoot> specification) where TAggregateRoot : class, IAggregateRoot
        {
            return GetRepository<TAggregateRoot>().Count(specification);
        }

        public async Task<long> CountAsync<TAggregateRoot>(ISpecification<TAggregateRoot> specification) where TAggregateRoot : class, IAggregateRoot
        {
            return await GetRepository<TAggregateRoot>().CountAsync(specification).ConfigureAwait(false);
        }

        public long Count<TAggregateRoot>(Expression<Func<TAggregateRoot, bool>> specification) where TAggregateRoot : class, IAggregateRoot
        {
            return GetRepository<TAggregateRoot>().Count(specification);
        }
        public async Task<long> CountAsync<TAggregateRoot>(Expression<Func<TAggregateRoot, bool>> specification) where TAggregateRoot : class, IAggregateRoot
        {
            return await GetRepository<TAggregateRoot>().CountAsync(specification).ConfigureAwait(false);
        }
        public IQueryable<TAggregateRoot> FindAll<TAggregateRoot>(params OrderExpression[] orderExpressions) where TAggregateRoot : class, IAggregateRoot
        {
            return GetRepository<TAggregateRoot>().FindAll(orderExpressions);
        }

        public IQueryable<TAggregateRoot> FindAll<TAggregateRoot>(ISpecification<TAggregateRoot> specification, params OrderExpression[] orderExpressions) where TAggregateRoot : class, IAggregateRoot
        {
            return GetRepository<TAggregateRoot>().FindAll(specification, orderExpressions);
        }

        public IQueryable<TAggregateRoot> FindAll<TAggregateRoot>(Expression<Func<TAggregateRoot, bool>> specification, params OrderExpression[] orderExpressions) where TAggregateRoot : class, IAggregateRoot
        {
            return GetRepository<TAggregateRoot>().FindAll(specification, orderExpressions);
        }

        public TAggregateRoot Find<TAggregateRoot>(ISpecification<TAggregateRoot> specification) where TAggregateRoot : class, IAggregateRoot
        {
            return GetRepository<TAggregateRoot>().Find(specification);
        }
        public async Task<TAggregateRoot> FindAsync<TAggregateRoot>(ISpecification<TAggregateRoot> specification) where TAggregateRoot : class, IAggregateRoot
        {
            return await GetRepository<TAggregateRoot>().FindAsync(specification);
        }
        public TAggregateRoot Find<TAggregateRoot>(Expression<Func<TAggregateRoot, bool>> specification) where TAggregateRoot : class, IAggregateRoot
        {
            return GetRepository<TAggregateRoot>().Find(specification);
        }

        public async Task<TAggregateRoot> FindAsync<TAggregateRoot>(Expression<Func<TAggregateRoot, bool>> specification) where TAggregateRoot : class, IAggregateRoot
        {
            return await GetRepository<TAggregateRoot>().FindAsync(specification).ConfigureAwait(false);
        }

        public bool Exists<TAggregateRoot>(ISpecification<TAggregateRoot> specification) where TAggregateRoot : class, IAggregateRoot
        {
            return GetRepository<TAggregateRoot>().Exists(specification);
        }
        public async Task<bool> ExistsAsync<TAggregateRoot>(ISpecification<TAggregateRoot> specification) where TAggregateRoot : class, IAggregateRoot
        {
            return await GetRepository<TAggregateRoot>().ExistsAsync(specification).ConfigureAwait(false);
        }
        public bool Exists<TAggregateRoot>(Expression<Func<TAggregateRoot, bool>> specification) where TAggregateRoot : class, IAggregateRoot
        {
            return GetRepository<TAggregateRoot>().Exists(specification);
        }
        public async Task<bool> ExistsAsync<TAggregateRoot>(Expression<Func<TAggregateRoot, bool>> specification) where TAggregateRoot : class, IAggregateRoot
        {
            return await GetRepository<TAggregateRoot>().ExistsAsync(specification).ConfigureAwait(false);
        }
        public void Remove<TAggregateRoot>(TAggregateRoot entity) where TAggregateRoot : class, IAggregateRoot
        {
            GetRepository<TAggregateRoot>().Remove(entity);
        }

        public void Remove<TAggregateRoot>(IEnumerable<TAggregateRoot> entities) where TAggregateRoot : class, IAggregateRoot
        {
            GetRepository<TAggregateRoot>().Remove(entities);
        }

        public void Update<TAggregateRoot>(TAggregateRoot entity) where TAggregateRoot : class, IAggregateRoot
        {
            GetRepository<TAggregateRoot>().Update(entity);
        }

        public IQueryable<TAggregateRoot> PageFind<TAggregateRoot>(int pageIndex, int pageSize, Expression<Func<TAggregateRoot, bool>> specification, params OrderExpression[] orderExpressions) where TAggregateRoot : class, IAggregateRoot
        {
            return GetRepository<TAggregateRoot>().PageFind(pageIndex, pageSize, specification, orderExpressions);
        }

        public IQueryable<TAggregateRoot> PageFind<TAggregateRoot>(int pageIndex, int pageSize, Expression<Func<TAggregateRoot, bool>> specification, ref long totalCount, params OrderExpression[] orderExpressions) where TAggregateRoot : class, IAggregateRoot
        {
            return GetRepository<TAggregateRoot>().PageFind(pageIndex, pageSize, specification, ref totalCount, orderExpressions);
        }

        public async Task<Tuple<IQueryable<TAggregateRoot>, long>> PageFindAsync<TAggregateRoot>(int pageIndex, int pageSize, Expression<Func<TAggregateRoot, bool>> specification, params OrderExpression[] orderExpressions) where TAggregateRoot : class, IAggregateRoot
        {
            return await GetRepository<TAggregateRoot>().PageFindAsync(pageIndex, pageSize, specification, orderExpressions).ConfigureAwait(false);
        }

        public IQueryable<TAggregateRoot> PageFind<TAggregateRoot>(int pageIndex, int pageSize, ISpecification<TAggregateRoot> specification, ref long totalCount, params OrderExpression[] orderExpressions) where TAggregateRoot : class, IAggregateRoot
        {
            return GetRepository<TAggregateRoot>().PageFind(pageIndex, pageSize, specification, ref totalCount, orderExpressions);
        }

        public async Task<Tuple<IQueryable<TAggregateRoot>, long>> PageFindAsync<TAggregateRoot>(int pageIndex, int pageSize, ISpecification<TAggregateRoot> specification, params OrderExpression[] orderExpressions) where TAggregateRoot : class, IAggregateRoot
        {
            return await GetRepository<TAggregateRoot>().PageFindAsync(pageIndex, pageSize, specification, orderExpressions).ConfigureAwait(false);
        }

        public IQueryable<TAggregateRoot> PageFind<TAggregateRoot>(int pageIndex, int pageSize, ISpecification<TAggregateRoot> specification, params OrderExpression[] orderExpressions) where TAggregateRoot : class, IAggregateRoot
        {
            return GetRepository<TAggregateRoot>().PageFind(pageIndex, pageSize, specification, orderExpressions);
        }

        #endregion

    }
}
