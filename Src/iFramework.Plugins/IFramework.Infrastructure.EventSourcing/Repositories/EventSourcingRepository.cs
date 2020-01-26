using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using IFramework.Event;
using IFramework.Infrastructure.EventSourcing.Domain;
using IFramework.Infrastructure.EventSourcing.Stores;
using IFramework.Repositories;
using IFramework.Specifications;
using IFramework.UnitOfWork;

namespace IFramework.Infrastructure.EventSourcing.Repositories
{
    public interface IEventSourcingRepository
    {
        EventSourcingEntityEntry[] GetEntries();
        void Reset();
    }

    public interface IEventSourcingRepository<TAggregateRoot> : 
        IRepository<TAggregateRoot>, IEventSourcingRepository
        where TAggregateRoot : class
    {

    }

    public class EventSourcingRepository<TAggregateRoot> :
        IEventSourcingRepository<TAggregateRoot>
        where TAggregateRoot : class, IEventSourcingAggregateRoot, new()
    {
        private readonly IEventStore _eventStore;
        private readonly InMemoryStore _inMemoryStore;
        private readonly Dictionary<string, EventSourcingEntityEntry> _local;
        private readonly ISnapshotStore _snapshotStore;

        public EventSourcingRepository(InMemoryStore inMemoryStore, ISnapshotStore snapshotStore, IEventStore eventStore, IEventSourcingUnitOfWork unitOfWork)
        {
            _local = new Dictionary<string, EventSourcingEntityEntry>();
            _inMemoryStore = inMemoryStore;
            _snapshotStore = snapshotStore;
            _eventStore = eventStore;
            (unitOfWork as UnitOfWork)?.RegisterRepositories(this);
        }

        public void Add(IEnumerable<TAggregateRoot> entities)
        {
            throw new NotImplementedException();
        }

        public void Add(TAggregateRoot entity)
        {
            _local[entity.Id] = new EventSourcingEntityEntry(entity);
        }

        public Task AddAsync(IEnumerable<TAggregateRoot> entities)
        {
            throw new NotImplementedException();
        }

        public Task AddAsync(TAggregateRoot entity)
        {
            Add(entity);
            return Task.CompletedTask;
        }

        public TAggregateRoot GetByKey(params object[] keyValues)
        {
            return GetByKeyAsync(keyValues)
                   .GetAwaiter()
                   .GetResult();
        }

        public async Task<TAggregateRoot> GetByKeyAsync(params object[] keyValues)
        {
            var id = string.Join(".", keyValues);
            if (_local.ContainsKey(id))
            {
                return _local[id].Entity as TAggregateRoot;
            }

            var ag = _inMemoryStore.Get<TAggregateRoot>(id);
            if (ag == null)
            {
                // get from snapshot store and replay with events
                ag = await _snapshotStore.GetAsync<TAggregateRoot>(id);
                var fromVersion = ag?.Version ?? 0;
                var events = await _eventStore.GetEvents(id, fromVersion)
                                              .ConfigureAwait(false);
                if (ag == null && events.Length > 0)
                {
                    ag = Activator.CreateInstance<TAggregateRoot>();
                }

                if (ag != null)
                {
                    if (events.Length > 0)
                    {
                        ag.Replay(events.Cast<IAggregateRootEvent>()
                                        .ToArray());
                    }

                    _inMemoryStore.Set(ag);
                    if (ag.Version > fromVersion)
                    {
                        await _snapshotStore.UpdateAsync(ag);
                    }
                }
            }

            if (ag != null)
            {
                _local[id] = new EventSourcingEntityEntry(ag, ag.Version);
            }

            return ag;
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
            _local[entity.Id].Deleted = true;
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

        public EventSourcingEntityEntry[] GetEntries()
        {
            return _local.Values.ToArray();
        }

        public void Reset()
        {
            _local.Clear();
        }
    }
}