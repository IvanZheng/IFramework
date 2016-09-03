using IFramework.Event;
using IFramework.Infrastructure;
using Sample.DomainEvents.Products;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.AsyncDomainEventSubscriber.Products
{
    public class ProductEventSubscriber :
      IEventSubscriber<ProductCreated>
    {
        IEventBus _EventBus;
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
