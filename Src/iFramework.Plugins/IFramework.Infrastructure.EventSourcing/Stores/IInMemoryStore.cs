using System;
using System.Collections.Generic;
using System.Text;
using IFramework.Infrastructure.EventSourcing.Domain;

namespace IFramework.Infrastructure.EventSourcing.Stores
{
    public interface IInMemoryStore
    {
        void Remove(string id);

        TAggregateRoot Get<TAggregateRoot>(string id) where TAggregateRoot : class;

        void Set<TAggregateRoot>(TAggregateRoot ag) where TAggregateRoot : class, IEventSourcingAggregateRoot, new();
    }
}
