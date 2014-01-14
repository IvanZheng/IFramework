using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFramework.Infrastructure;
using IFramework.Message;
using IFramework.Message.Impl;
using System.Collections;
using IFramework.Event;
using IFramework.MessageQueue.MessageFormat;

namespace IFramework.MessageQueue.ZeroMQ
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

        public void Publish<TEvent>(TEvent @event) where TEvent : IDomainEvent
        {
            var eventSubscribers = EventSubscriberProvider.GetHandlers(@event.GetType());
            eventSubscribers.ForEach(eventSubscriber => {
                eventSubscriber.Handle(@event);
            });
            DomainEventContextQueue.Enqueue(new MessageContext(@event));
        }

        public void Publish<TEvent>(IEnumerable<TEvent> eventContexts) where TEvent : IDomainEvent
        {
            eventContexts.ForEach(@event => Publish(@event));
        }

        public virtual void Dispose()
        {
            
        }

        public IEnumerable<IMessageContext> GetMessageContexts()
        {
            return DomainEventContextQueue.Cast<IMessageContext>();
        }
    }
}
