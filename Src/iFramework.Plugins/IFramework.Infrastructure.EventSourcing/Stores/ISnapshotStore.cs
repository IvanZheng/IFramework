﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using IFramework.Infrastructure.EventSourcing.Domain;

namespace IFramework.Infrastructure.EventSourcing.Stores
{
    public interface ISnapshotStore
    {
        Task<TAggregateRoot> GetAsync<TAggregateRoot>(string id) where TAggregateRoot : IEventSourcingAggregateRoot;
        Task UpdateAsync(IEventSourcingAggregateRoot ar);
        Task Connect();
    }
}
