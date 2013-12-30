using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFramework.Infrastructure;
using IFramework.Message;
using IFramework.Message.Impl;
using System.Collections;

namespace IFramework.Event.Impl
{
    public class DomainEventBus : IDomainEventBus
    {
        protected Queue DomainEventContextQueue;
        protected IEventSubscriberProvider EventSubscriberProvider { get; set; }
        protected IEventPublisher EventPublisher { get; set; }

        public DomainEventBus(IEventSubscriberProvider provider, IEventPublisher eventPublisher)
        {
            EventPublisher = eventPublisher;
            EventSubscriberProvider = provider;
            DomainEventContextQueue = Queue.Synchronized(new Queue());
        }

        public virtual void Commit()
        {
            EventPublisher.Publish(DomainEventContextQueue.ToArray());
        }

        public void Publish<TEvent>(TEvent eventContext) where TEvent : IMessageContext
        {
            var @event = eventContext.Message;
            var eventSubscribers = EventSubscriberProvider.GetHandlers(@event.GetType());
            eventSubscribers.ForEach(eventSubscriber => {
                eventSubscriber.Handle(@event);
            });
            DomainEventContextQueue.Enqueue(eventContext);
        }

        public void Publish<TEvent>(IEnumerable<TEvent> eventContexts) where TEvent : IMessageContext
        {
            eventContexts.ForEach(@eventContext => Publish(@eventContext));
        }

        public virtual void Dispose()
        {
            
        }


        public IEnumerable<IMessageContext> GetMessages()
        {
            return DomainEventContextQueue.Cast<IMessageContext>();
        }
    }
}
