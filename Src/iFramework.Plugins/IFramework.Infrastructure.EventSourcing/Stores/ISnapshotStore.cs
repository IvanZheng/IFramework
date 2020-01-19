using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using IFramework.Infrastructure.EventSourcing.Domain;

namespace IFramework.Infrastructure.EventSourcing.Stores
{
    public interface ISnapshotStore
    {
        Task<TAggregateRoot> GetAsync<TAggregateRoot>(string id) where TAggregateRoot : EventSourcingAggregateRoot;
        Task UpdateAsync(EventSourcingAggregateRoot ar);
        Task Connect();
    }
}
