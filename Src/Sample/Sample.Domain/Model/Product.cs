using System;
using IFramework.Domain;
using IFramework.Event;
using IFramework.Message;
using Sample.DomainEvents;
using Sample.DomainEvents.Products;

namespace Sample.Domain.Model
{
    public class Product : TimestampedAggregateRoot
    {
        public Product() { }

        public Product(Guid id, string name, int count)
        {
            OnEvent(new ProductCreated(id, name, count, DateTime.Now));
        }

        public Guid Id { get; protected set; }
        public string Name { get; protected set; }
        public int Count { get; protected set; }
        public DateTime CreateTime { get; protected set; }

        void Handle(ProductCreated @event)
        {
            Id = (Guid) @event.AggregateRootId;
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
            var count = Count - reduceCount;
            if (count < 0)
            {
                OnException(new ProductCountNotEnough(Id, reduceCount, Count));
            }
            Count = count;
        }
    }
}