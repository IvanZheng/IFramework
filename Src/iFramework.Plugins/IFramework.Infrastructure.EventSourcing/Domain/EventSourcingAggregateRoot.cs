using System.Collections.Generic;
using System.Linq;
using IFramework.Domain;
using IFramework.Event;

namespace IFramework.Infrastructure.EventSourcing.Domain
{
    public interface IEventSourcingAggregateRoot : IAggregateRoot
    {
        string Id { get; }
        int Version { get; set; }
        void Replay(params IAggregateRootEvent[] events);
        IEnumerable<IAggregateRootEvent> GetDomainEvents();
        void Rollback();
    }

    public class EventSourcingAggregateRoot : VersionedAggregateRoot, IEventSourcingAggregateRoot
    {
        internal EventSourcingAggregateRoot(IAggregateRootEvent[] events)
        {
            Replay(events);
        }

        public string Id { get; protected set; }

        int IEventSourcingAggregateRoot.Version
        {
            get => Version;
            set => Version = value;
        }

        public void Replay(params IAggregateRootEvent[] events)
        {
            foreach (var @event in events.OrderBy(e => e.Version))
            {
                base.OnEvent(@event);
                Version = @event.Version;
            }
        }
    }
}