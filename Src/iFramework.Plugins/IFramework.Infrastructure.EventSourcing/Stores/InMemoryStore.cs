using System.Collections.Concurrent;
using System.Collections.Generic;
using IFramework.Infrastructure.EventSourcing.Domain;

namespace IFramework.Infrastructure.EventSourcing.Stores
{
    public class InMemoryStore
    {
        private readonly IDictionary<string, string> _aggregateRootSet;
        public InMemoryStore(int maxCapacity = 100000)
        {
            _aggregateRootSet = new ConcurrentLimitedSizeDictionary<string, string>(maxCapacity);
        }

        public void Remove(string id)
        {
            _aggregateRootSet.Remove(id);
        }

        public TAggregateRoot Get<TAggregateRoot>(string id) where TAggregateRoot : class
        {
            TAggregateRoot aggregateRoot = default;
            var key = FormatStoreKey<TAggregateRoot>(id);
            var aggregateRootContent = _aggregateRootSet.TryGetValue(key);
            if (!string.IsNullOrWhiteSpace(aggregateRootContent))
            {
                aggregateRoot = aggregateRootContent.ToJsonObject<TAggregateRoot>();
            }

            return aggregateRoot;
        }

        private string FormatStoreKey<TAggregateRoot>(string id) where TAggregateRoot : class
        {
            var key = $"{typeof(TAggregateRoot).Name}.{id}";
            return key;
        }

        public void Set<TAggregateRoot>(TAggregateRoot ag) where TAggregateRoot : class, IEventSourcingAggregateRoot, new()
        {
            var key = FormatStoreKey<TAggregateRoot>(ag.Id);
            _aggregateRootSet[key] = ag.ToJson();
        }
    }
}