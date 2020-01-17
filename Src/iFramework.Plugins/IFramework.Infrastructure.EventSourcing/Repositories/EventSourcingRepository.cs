using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using IFramework.Event;
using IFramework.Infrastructure.EventSourcing.Stores;
using IFramework.Repositories;
using IFramework.Specifications;

namespace IFramework.Infrastructure.EventSourcing.Repositories
{
    public class EventSourcingRepository<TAggregateRoot> :
        IRepository<TAggregateRoot> 
        where TAggregateRoot : class
    {
        private InMemoryStore _inMemoryStore;
        private ISnapshotStore _snapshotStore;
        private IEventStore _eventStore;

        public EventSourcingRepository(InMemoryStore inMemoryStore, ISnapshotStore snapshotStore, IEventStore eventStore)
        {
            _inMemoryStore = inMemoryStore;
            _snapshotStore = snapshotStore;
            _eventStore = eventStore;
        }

        public void Add(IEnumerable<TAggregateRoot> entities)
        {
            throw new NotImplementedException();
        }

        public void Add(TAggregateRoot entity)
        {
            throw new NotImplementedException();
        }

        public Task AddAsync(IEnumerable<TAggregateRoot> entities)
        {
            throw new NotImplementedException();
        }

        public Task AddAsync(TAggregateRoot entity)
        {
            throw new NotImplementedException();
        }

        public TAggregateRoot GetByKey(params object[] keyValues)
        {
            throw new NotImplementedException();
        }

        public Task<TAggregateRoot> GetByKeyAsync(params object[] keyValues)
        {
            throw new NotImplementedException();
        }

        public long Count(ISpecification<TAggregateRoot> specification)
        {
            throw new NotImplementedException();
        }

        public Task<long> CountAsync(ISpecification<TAggregateRoot> specification)
        {
            throw new NotImplementedException();
        }

        public long Count(Expression<Func<TAggregateRoot, bool>> specification)
        {
            throw new NotImplementedException();
        }

        public Task<long> CountAsync(Expression<Func<TAggregateRoot, bool>> specification)
        {
            throw new NotImplementedException();
        }

        public long Count()
        {
            throw new NotImplementedException();
        }

        public Task<long> CountAsync()
        {
            throw new NotImplementedException();
        }

        public IQueryable<TAggregateRoot> FindAll(params OrderExpression[] orderByExpressions)
        {
            throw new NotImplementedException();
        }

        public IQueryable<TAggregateRoot> FindAll(ISpecification<TAggregateRoot> specification, params OrderExpression[] orderByExpressions)
        {
            throw new NotImplementedException();
        }

        public IQueryable<TAggregateRoot> FindAll(Expression<Func<TAggregateRoot, bool>> specification, params OrderExpression[] orderByExpressions)
        {
            throw new NotImplementedException();
        }

        public TAggregateRoot Find(ISpecification<TAggregateRoot> specification)
        {
            throw new NotImplementedException();
        }

        public Task<TAggregateRoot> FindAsync(ISpecification<TAggregateRoot> specification)
        {
            throw new NotImplementedException();
        }

        public TAggregateRoot Find(Expression<Func<TAggregateRoot, bool>> specification)
        {
            throw new NotImplementedException();
        }

        public Task<TAggregateRoot> FindAsync(Expression<Func<TAggregateRoot, bool>> specification)
        {
            throw new NotImplementedException();
        }

        public bool Exists(ISpecification<TAggregateRoot> specification)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ExistsAsync(ISpecification<TAggregateRoot> specification)
        {
            throw new NotImplementedException();
        }

        public bool Exists(Expression<Func<TAggregateRoot, bool>> specification)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ExistsAsync(Expression<Func<TAggregateRoot, bool>> specification)
        {
            throw new NotImplementedException();
        }

        public void Remove(TAggregateRoot entity)
        {
            throw new NotImplementedException();
        }

        public void Remove(IEnumerable<TAggregateRoot> entities)
        {
            throw new NotImplementedException();
        }

        public void Reload(TAggregateRoot entity)
        {
            throw new NotImplementedException();
        }

        public Task ReloadAsync(TAggregateRoot entity)
        {
            throw new NotImplementedException();
        }

        public void Update(TAggregateRoot entity)
        {
            throw new NotImplementedException();
        }

        public (IQueryable<TAggregateRoot> DataQueryable, long Total) PageFind(int pageIndex, int pageSize, Expression<Func<TAggregateRoot, bool>> expression, params OrderExpression[] orderByExpressions)
        {
            throw new NotImplementedException();
        }

        public Task<(IQueryable<TAggregateRoot> DataQueryable, long Total)> PageFindAsync(int pageIndex, int pageSize, Expression<Func<TAggregateRoot, bool>> specification, params OrderExpression[] orderByExpressions)
        {
            throw new NotImplementedException();
        }

        public (IQueryable<TAggregateRoot> DataQueryable, long Total) PageFind(int pageIndex, int pageSize, ISpecification<TAggregateRoot> specification, params OrderExpression[] orderByExpressions)
        {
            throw new NotImplementedException();
        }

        public Task<(IQueryable<TAggregateRoot> DataQueryable, long Total)> PageFindAsync(int pageIndex, int pageSize, ISpecification<TAggregateRoot> specification, params OrderExpression[] orderByExpressions)
        {
            throw new NotImplementedException();
        }
    }
}
