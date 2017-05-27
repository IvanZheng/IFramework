using System;
using IFramework.Domain;
using IFramework.Event;
using IFramework.Message;
using Sample.DomainEvents;
using Sample.DomainEvents.Products;
using Sample.DTO;

namespace Sample.Domain.Model
{
    public class Product : TimestampedAggregateRoot,
        IEventSubscriber<ProductCreated>
    {
        public Product()
        {
        }

        public Product(Guid id, string name, int count)
        {
            OnEvent(new ProductCreated(id, name, count, DateTime.Now));
        }

        public Guid Id { get; set; }
        public string Name { get; set; }
        public int Count { get; set; }
        public DateTime CreateTime { get; set; }

        void IMessageHandler<ProductCreated>.Handle(ProductCreated @event)
        {
            Id = (Guid) @event.AggregateRootID;
            Name = @event.Name;
            Count = @event.Count;
            CreateTime = @event.CreateTime;
        }

        public void SetCount(int count)
        {
            Count = count;
        }

        public void ReduceCount(int reduceCount)
        {
            Count = Count - reduceCount;
            if (Count < 0)
                throw new SampleDomainException(ErrorCode.CountNotEnougth);
        }
    }
}