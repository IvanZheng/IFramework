using IFramework.Event;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace IFramework.Domain
{
    public class VersionedAggregateRoot : AggregateRoot
    {
        int _newVersion;
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

        public override void Rollback()
        {
            _newVersion = 0;
            base.Rollback();
        }

        [ConcurrencyCheck]
        public int Version
        {
            get;
            private set;
        }

        protected override void OnEvent<TDomainEvent>(TDomainEvent @event)
        {
            @event.Version = NewVersion;
            Version = NewVersion;
            base.OnEvent<TDomainEvent>(@event);
        }
    }
}
