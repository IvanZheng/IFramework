using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using IFramework.Event;
using IFramework.Exceptions;
using IFramework.Infrastructure;

namespace IFramework.Domain
{
    public abstract class AggregateRoot : Entity, IAggregateRoot
    {
        private string _aggregateRootType;

        private Queue<IAggregateRootEvent> _eventQueue;
        private Queue<IAggregateRootEvent> EventQueue => _eventQueue ?? (_eventQueue = new Queue<IAggregateRootEvent>());

        private string AggregateRootName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_aggregateRootType))
                {
                    var aggreagetRootType = GetType();
                    if ("EntityProxyModule" == GetType().Module.ToString() && aggreagetRootType.BaseType != null)
                    {
                        aggreagetRootType = aggreagetRootType.BaseType;
                    }
                    _aggregateRootType = aggreagetRootType.FullName;
                }
                return _aggregateRootType;
            }
        }

        public IEnumerable<IAggregateRootEvent> GetDomainEvents()
        {
            return EventQueue.ToList();
        }

        public void ClearDomainEvents()
        {
            EventQueue.Clear();
        }

        public virtual void Rollback()
        {
            ClearDomainEvents();
        }

        protected virtual void OnEvent<TDomainEvent>(TDomainEvent @event) where TDomainEvent : class, IAggregateRootEvent
        {
            HandleEvent(@event);
            @event.AggregateRootName = AggregateRootName;
            EventQueue.Enqueue(@event);
        }

        protected virtual void OnException<TDomainException>(TDomainException exception) where TDomainException : IAggregateRootExceptionEvent
        {
            exception.AggregateRootName = AggregateRootName;
            throw new DomainException(exception);
        }

        protected void HandleEvent<TDomainEvent>(TDomainEvent @event) where TDomainEvent : class, IAggregateRootEvent
        {
            var args = new object[] {@event};
            var handler = GetType().GetMethodInfo("Handle", args);
            if (handler != null)
            {
                handler.Invoke(this, args);
            }
            
            //else no need to call parent event handler, let client decide it!
            //{
            //    var eventSubscriberInterfaces = this.GetType().GetInterfaces()
            //        .Where(i => i.IsGenericType)
            //        .Where(i => i.GetGenericTypeDefinition() == typeof(IEventSubscriber<>).GetGenericTypeDefinition())
            //        .ForEach(i => {
            //            var eventType = i.GetGenericArguments().First();
            //            if (eventType.IsAssignableFrom(typeof(TDomainEvent)))
            //            {
            //                i.GetMethod("Handle").Invoke(this, new object[] { @event });
            //            }
            //        });
            //}
        }

      
    }
}