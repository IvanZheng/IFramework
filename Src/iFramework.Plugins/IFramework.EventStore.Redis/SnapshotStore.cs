using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using IFramework.Infrastructure.EventSourcing.Domain;
using IFramework.Infrastructure.EventSourcing.Stores;

namespace IFramework.EventStore.Redis
{
    public class SnapshotStore:ISnapshotStore
    {
        public Task<TAggregateRoot> GetAsync<TAggregateRoot>(string id) where TAggregateRoot : EventSourcingAggregateRoot
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(EventSourcingAggregateRoot ar)
        {
            throw new NotImplementedException();
        }
    }
}
