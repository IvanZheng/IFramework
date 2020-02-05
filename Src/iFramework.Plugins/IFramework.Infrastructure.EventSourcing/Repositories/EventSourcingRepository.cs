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

    public interface IEventSourcingRepository<TAggregateRoot> : IEventSourcingRepository
        where TAggregateRoot : class
    {
        void Add(TAggregateRoot entity);
        TAggregateRoot GetByKey(params object[] keyValues);
        Task<TAggregateRoot> GetByKeyAsync(params object[] keyValues);
        void Remove(TAggregateRoot entity);
    }

    public class EventSourcingRepository<TAggregateRoot> :
        IEventSourcingRepository<TAggregateRoot>
        where TAggregateRoot : class, IEventSourcingAggregateRoot, new()
    {
        private readonly IEventStore _eventStore;
        private readonly IInMemoryStore _inMemoryStore;
        private readonly Dictionary<string, EventSourcingEntityEntry> _local;
        private readonly ISnapshotStore _snapshotStore;

        public EventSourcingRepository(IInMemoryStore inMemoryStore, ISnapshotStore snapshotStore, IEventStore eventStore, IEventSourcingUnitOfWork unitOfWork)
        {
            _local = new Dictionary<string, EventSourcingEntityEntry>();
            _inMemoryStore = inMemoryStore;
            _snapshotStore = snapshotStore;
            _eventStore = eventStore;
            (unitOfWork as UnitOfWork)?.RegisterRepositories(this);
        }

        public void Add(TAggregateRoot entity)
        {
            _local[entity.Id] = new EventSourcingEntityEntry(entity);
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
                var events = await _eventStore.GetEvents(id, fromVersion + 1)
                                              .ConfigureAwait(false);
                if (ag == null && events.Length > 0)
                {
                    ag = Activator.CreateInstance<TAggregateRoot>();
                }

                if (ag != null)
                {
                    if (events.Length > 0)
                    {
                        ag.Replay(events.OfType<IAggregateRootEvent>()
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

        public void Remove(TAggregateRoot entity)
        {
            _local[entity.Id].Deleted = true;
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