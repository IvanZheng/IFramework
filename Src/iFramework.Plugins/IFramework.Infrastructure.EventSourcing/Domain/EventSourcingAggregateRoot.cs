using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFramework.Domain;
using IFramework.Event;

namespace IFramework.Infrastructure.EventSourcing.Domain
{
    public class EventSourcingAggregateRoot: VersionedAggregateRoot
    {
        public string Id { get; protected set; }

        internal EventSourcingAggregateRoot(IAggregateRootEvent[] events)
        {
            Replay(events);
        }

        internal void Replay(params IAggregateRootEvent[] events)
        {
            foreach (var @event in events.OrderBy(e => e.Version))
            {
                base.OnEvent(@event);
                Version = @event.Version;
            }
        }
    }
}
