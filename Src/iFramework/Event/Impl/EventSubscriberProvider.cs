using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.Event.Impl
{
    public class EventSubscriberProvider : Message.Impl.HandlerProvider<IEventSubscriber<IDomainEvent>>, IEventSubscriberProvider
    {
        public EventSubscriberProvider(params string[] assemblies)
            : base(assemblies)
        {

        }
    }
}
