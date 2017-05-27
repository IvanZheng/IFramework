using System;
using IFramework.Event;
using IFramework.Infrastructure;
using Sample.DomainEvents.Products;

namespace Sample.AsyncDomainEventSubscriber.Products
{
    public class ProductEventSubscriber :
        IEventSubscriber<ProductCreated>
    {
        private readonly IEventBus _EventBus;

        public ProductEventSubscriber(IEventBus eventBus)
        {
            _EventBus = eventBus;
        }

        public void Handle(ProductCreated @event)
        {
            Console.Write("subscriber1: {0} has registered.", @event.ToJson());

            _EventBus.FinishSaga(@event);
        }
    }
}