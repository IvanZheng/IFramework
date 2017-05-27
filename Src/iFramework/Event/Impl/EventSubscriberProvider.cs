using System;
using System.Linq;
using IFramework.Message.Impl;

namespace IFramework.Event.Impl
{
    public class EventSubscriberProvider : HandlerProvider, IEventSubscriberProvider
    {
        private Type[] _HandlerGenericTypes;

        public EventSubscriberProvider(params string[] assemblies)
            : base(assemblies)
        {
        }

        protected override Type[] HandlerGenericTypes
        {
            get
            {
                return _HandlerGenericTypes ?? (_HandlerGenericTypes = new[]
                           {
                               typeof(IEventSubscriber<IEvent>),
                               typeof(IEventAsyncSubscriber<IEvent>)
                           }
                           .Select(ht => ht.GetGenericTypeDefinition())
                           .ToArray());
            }
        }
    }
}