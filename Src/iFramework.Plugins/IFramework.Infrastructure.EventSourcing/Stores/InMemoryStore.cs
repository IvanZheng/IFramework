using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using IFramework.Infrastructure.EventSourcing.Domain;

namespace IFramework.Infrastructure.EventSourcing.Stores
{
    public class InMemoryStore: IInMemoryStore
    {
        private readonly IDictionary<string, string> _aggregateRootSet;
        public InMemoryStore(int maxCapacity = 100000)
        {
            _aggregateRootSet = new ConcurrentLimitedSizeDictionary<string, string>(maxCapacity);
        }

        public void Remove(IEventSourcingAggregateRoot aggregateRoot)
        {
            var key = FormatStoreKey(aggregateRoot.GetType(), aggregateRoot.Id);
            _aggregateRootSet.Remove(key);
        }

        public TAggregateRoot Get<TAggregateRoot>(string id) where TAggregateRoot : class
        {
            TAggregateRoot aggregateRoot = default;
            var key = FormatStoreKey<TAggregateRoot>(id);
            var aggregateRootContent = _aggregateRootSet.TryGetValue(key);
            if (!string.IsNullOrWhiteSpace(aggregateRootContent))
            {
                aggregateRoot = aggregateRootContent.ToJsonObject<TAggregateRoot>(true);
            }

            return aggregateRoot;
        }

        private string FormatStoreKey(Type aggregateRootType, string id)
        {
            var key = $"{aggregateRootType.Name}.{id}";
            return key;
        }

        private string FormatStoreKey<TAggregateRoot>(string id)
        {
            var key = $"{typeof(TAggregateRoot).Name}.{id}";
            return key;
        }

        public void Set(IEventSourcingAggregateRoot ag)
        {
            var key = FormatStoreKey(ag.GetType() , ag.Id);
            _aggregateRootSet[key] = ag.ToJson();
        }
    }
}