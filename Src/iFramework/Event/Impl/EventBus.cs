using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFramework.Infrastructure;

namespace IFramework.Event.Impl
{
    public class EventBus : IEventBus
    {
        protected List<IEvent> EventQueue;
        protected IEventSubscriberProvider EventSubscriberProvider { get; set; }
        public EventBus(IEventSubscriberProvider provider)
        {
            EventSubscriberProvider = provider;
            EventQueue = new List<IEvent>();
        }

        public virtual void Commit()
        {
        }

        public void Publish<TEvent>(TEvent @event) where TEvent : IEvent
        {
            EventQueue.Add(@event);
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

        public void Publish<TEvent>(IEnumerable<TEvent> events) where TEvent : IEvent
        {
            events.ForEach(@event => Publish(@event));
        }

        public virtual void Dispose()
        {

        }

        public IEnumerable<IEvent> GetMessages()
        {
            return EventQueue;
        }
        
        public void ClearMessages()
        {
            EventQueue.Clear();
        }
    }
}
