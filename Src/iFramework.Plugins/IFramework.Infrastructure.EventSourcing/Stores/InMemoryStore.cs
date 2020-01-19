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