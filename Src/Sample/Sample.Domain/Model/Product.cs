using IFramework.Domain;
using IFramework.Event;
using IFramework.Message;
using IFramework.SysExceptions;
using Sample.DomainEvents.Products;
using Sample.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Domain.Model
{
    public class Product : TimestampedAggregateRoot,
       IEventSubscriber<ProductCreated>
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int Count { get; set; }
        public DateTime CreateTime { get; set; }
        public Product() { }
        public Product(Guid id, string name, int count)
        {
            OnEvent(new ProductCreated(id, name, count, DateTime.Now));
        }

        void IMessageHandler<ProductCreated>.Handle(ProductCreated @event)
        {
            Id = (Guid)@event.AggregateRootID;
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
            {
                throw new SysException(ErrorCode.CountNotEnougth);
            }
        }


    }
}
