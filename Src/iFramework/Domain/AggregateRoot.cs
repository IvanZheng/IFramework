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

        string _aggreagetRootType;
        protected string AggregateRootName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_aggreagetRootType))
                {
                    var aggreagetRootType = this.GetType();
                    if ("EntityProxyModule" == this.GetType().Module.ToString())
                    {
                        aggreagetRootType = aggreagetRootType.BaseType;
                    }
                    _aggreagetRootType = aggreagetRootType.FullName;
                }
                return _aggreagetRootType;
            }
        }

        protected void OnEvent<TDomainEvent>(TDomainEvent @event) where TDomainEvent : class, IDomainEvent
        {
            HandleEvent<TDomainEvent>(@event);
            @event.AggregateRootName = AggregateRootName;
            EventBus.Publish(@event);
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
