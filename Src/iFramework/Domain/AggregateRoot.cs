using System.Collections.Generic;
using System.Linq;
using IFramework.Event;

namespace IFramework.Domain
{
    public abstract class AggregateRoot : Entity, IAggregateRoot
    {
        private readonly Queue<IDomainEvent> _eventQueue = new Queue<IDomainEvent>();
        private string _aggreagetRootType;

        private string AggregateRootName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_aggreagetRootType))
                {
                    var aggreagetRootType = GetType();
                    if ("EntityProxyModule" == GetType().Module.ToString())
                    {
                        aggreagetRootType = aggreagetRootType.BaseType;
                    }
                    _aggreagetRootType = aggreagetRootType.FullName;
                }
                return _aggreagetRootType;
            }
        }

        public IEnumerable<IDomainEvent> GetDomainEvents()
        {
            return _eventQueue.ToList();
        }

        public virtual void Rollback()
        {
            _eventQueue.Clear();
        }

        protected virtual void OnEvent<TDomainEvent>(TDomainEvent @event) where TDomainEvent : class, IDomainEvent
        {
            HandleEvent(@event);
            @event.AggregateRootName = AggregateRootName;
            _eventQueue.Enqueue(@event);
        }

        private void HandleEvent<TDomainEvent>(TDomainEvent @event) where TDomainEvent : class, IDomainEvent
        {
            var subscriber = this as IEventSubscriber<TDomainEvent>;
            subscriber?.Handle(@event);
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