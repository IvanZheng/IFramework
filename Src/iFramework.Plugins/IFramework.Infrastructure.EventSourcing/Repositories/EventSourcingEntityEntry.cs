using System;
using System.Collections.Generic;
using System.Text;
using IFramework.Infrastructure.EventSourcing.Domain;
using Microsoft.EntityFrameworkCore;

namespace IFramework.Infrastructure.EventSourcing.Repositories
{
    public class EventSourcingEntityEntry
    {
        public int Version { get; set; }
        public IEventSourcingAggregateRoot Entity { get; set; }

        internal bool Deleted { get; set; }
        public EntityState EntityState
        {
            get
            {
                if (Deleted)
                {
                    return EntityState.Deleted;
                }
                else if (Version == 0)
                {
                    return EntityState.Added;
                }
                else if (Entity.Version > Version)
                {
                    return EntityState.Modified;
                }
                else
                {
                    return EntityState.Unchanged;
                }
            }
        }

        public EventSourcingEntityEntry(IEventSourcingAggregateRoot entity, int version = 0)
        {
            Version = version;
            Entity = entity;
        }
    }
}
