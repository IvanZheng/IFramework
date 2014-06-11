using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFramework.Infrastructure;

namespace IFramework.Event.Impl
{
    public class DomainEventBus : IDomainEventBus
    {
        protected ConcurrentQueue<IDomainEvent> DomainEventQueue;
        protected IEventSubscriberProvider EventSubscriberProvider { get; set; }
        public DomainEventBus(IEventSubscriberProvider provider)
        {
            EventSubscriberProvider = provider;
            DomainEventQueue = new ConcurrentQueue<IDomainEvent>();
        }

        public virtual void Commit()
        {
        }

        public void Publish<TEvent>(TEvent @event) where TEvent : IDomainEvent
        {
            DomainEventQueue.Enqueue(@event);
            if (EventSubscriberProvider != null)
            {
                var eventSubscriberTypes = EventSubscriberProvider.GetHandlerTypes(@event.GetType());
                eventSubscriberTypes.ForEach(eventSubscriberType =>
                {
                    var eventSubscriber = IoCFactory.Resolve(eventSubscriberType);
                    ((dynamic)eventSubscriber).Handle((dynamic)@event);
                });
            }
        }

        public void Publish<TEvent>(IEnumerable<TEvent> events) where TEvent : IDomainEvent
        {
            events.ForEach(@event => Publish(@event));
        }

        public virtual void Dispose()
        {

        }

        public IEnumerable<IDomainEvent> GetMessages()
        {
            return DomainEventQueue;
        }
    }
}
