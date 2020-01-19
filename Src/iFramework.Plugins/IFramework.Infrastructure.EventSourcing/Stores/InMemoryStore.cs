using System.Collections.Concurrent;
using System.Collections.Generic;
using IFramework.Infrastructure.EventSourcing.Domain;

namespace IFramework.Infrastructure.EventSourcing.Stores
{
    public class InMemoryStore
    {
        private readonly int _maxCapacity;
        private readonly ConcurrentDictionary<string, string> _aggregateRootSet = new ConcurrentDictionary<string, string>();
        public InMemoryStore(int maxCapacity)
        {
            _maxCapacity = maxCapacity;
        }

        public TAggregateRoot Get<TAggregateRoot>(string id) where TAggregateRoot : class
        {
            TAggregateRoot aggregateRoot = default;
            var aggregateRootContent = _aggregateRootSet.TryGetValue(id);
            if (!string.IsNullOrWhiteSpace(aggregateRootContent))
            {
                aggregateRoot = aggregateRootContent.ToJsonObject<TAggregateRoot>();
            }

            return aggregateRoot;
        }

        public void Set<TAggregateRoot>(TAggregateRoot ag) where TAggregateRoot : EventSourcingAggregateRoot, new()
        {
            _aggregateRootSet[ag.Id] = ag.ToJson();
        }
    }
}