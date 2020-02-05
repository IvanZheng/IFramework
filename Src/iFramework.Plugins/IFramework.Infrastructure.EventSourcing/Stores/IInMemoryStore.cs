using System;
using System.Collections.Generic;
using System.Text;
using IFramework.Infrastructure.EventSourcing.Domain;

namespace IFramework.Infrastructure.EventSourcing.Stores
{
    public interface IInMemoryStore
    {
        void Remove(IEventSourcingAggregateRoot aggregateRoot);

        TAggregateRoot Get<TAggregateRoot>(string id) where TAggregateRoot : class;

        void Set(IEventSourcingAggregateRoot ag);
    }
}
