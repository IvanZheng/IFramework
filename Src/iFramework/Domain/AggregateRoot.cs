using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFramework.Infrastructure;
using Microsoft.Practices.Unity;
using System.Reflection;
using IFramework.Event;
using IFramework.Event.Impl;

namespace IFramework.Domain
{
    public abstract class AggregateRoot : Entity, IAggregateRoot
    {
        //// per request life time 
        IDomainEventBus EventBus
        {
            get
            {
                return IoCFactory.Resolve<IDomainEventBus>();
            }
        }

        protected void OnEvent<TDomainEvent>(TDomainEvent @event) where TDomainEvent : class, IDomainEvent
        {
            HandleEvent<TDomainEvent>(@event);
            EventBus.Publish(new DomainEventContext(@event, this.GetType()));
        }

        private void HandleEvent<TDomainEvent>(TDomainEvent @event) where TDomainEvent : class, IDomainEvent
        {
            var subscriber = this as IEventSubscriber<TDomainEvent>;
            if (subscriber != null)
            {
                subscriber.Handle(@event);
            }
            
        }
    }
}
