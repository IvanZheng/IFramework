using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.Event.Impl
{
    public class EventSubscriberProvider : Message.Impl.HandlerProvider, IEventSubscriberProvider
    {
        public EventSubscriberProvider(params string[] assemblies)
            : base(assemblies)
        {

        }

        Type[] _HandlerGenericTypes;

        protected override Type[] HandlerGenericTypes
        {
            get
            {
                return _HandlerGenericTypes ?? (_HandlerGenericTypes = new Type[]{
                                                                               typeof(IEventSubscriber<IEvent>),
                                                                               typeof(IEventAsyncSubscriber<IEvent>)  }
                                                                            .Select(ht => ht.GetGenericTypeDefinition())
                                                                            .ToArray());

            }
        }
    }
}
