using IFramework.Event;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;

namespace IFramework.Domain
{
    public class VersionedAggregateRoot : AggregateRoot
    {
        int _newVersion;
        [NotMapped]
        int NewVersion
        {
            get
            {
                if (_newVersion == 0)
                {
                    _newVersion = Version + 1;
                }
                return _newVersion;
            }
        }
        [ConcurrencyCheck]
        public int Version
        {
            get;
            private set;
        }

        protected override void OnEvent<TDomainEvent>(TDomainEvent @event)
        {
            if (@event is DomainEvent)
            {
                (@event as DomainEvent).Version = NewVersion;
            }
            Version = NewVersion;
            base.OnEvent<TDomainEvent>(@event);
        }
    }
}
