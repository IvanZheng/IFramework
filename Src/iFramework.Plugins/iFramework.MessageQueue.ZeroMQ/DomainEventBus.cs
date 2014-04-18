using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFramework.Infrastructure;
using IFramework.Message;
using IFramework.Message.Impl;
using System.Collections;
using IFramework.Event;
using System.Collections.Concurrent;
using IFramework.MessageQueue.MessageFormat;

namespace IFramework.MessageQueue.ZeroMQ
{
    public class DomainEventBus : IDomainEventBus
    {
        protected ConcurrentQueue<IMessageContext> DomainEventContextQueue;
        protected IEventSubscriberProvider EventSubscriberProvider { get; set; }
        protected IEventPublisher EventPublisher { get; set; }
        public DomainEventBus(IEventSubscriberProvider provider, IEventPublisher eventPublisher)
        {
            EventPublisher = eventPublisher;
            EventSubscriberProvider = provider;
            DomainEventContextQueue = new ConcurrentQueue<IMessageContext>();
        }

        public virtual void Commit()
        {
            EventPublisher.Publish(DomainEventContextQueue.ToArray());
        }

        public void Publish<TEvent>(TEvent @event) where TEvent : IDomainEvent
        {
            DomainEventContextQueue.Enqueue(new MessageContext(@event));
            var eventSubscriberTypes = EventSubscriberProvider.GetHandlerTypes(@event.GetType());
            eventSubscriberTypes.ForEach(eventSubscriberType =>
            {
                var eventSubscriber = IoCFactory.Resolve(eventSubscriberType);
                ((dynamic)eventSubscriber).Handle((dynamic)@event);
            });
            
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
            return DomainEventContextQueue;
        }
    }
}
