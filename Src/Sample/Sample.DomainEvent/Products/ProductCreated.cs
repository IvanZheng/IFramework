using IFramework.Message;
using System;

namespace Sample.DomainEvents.Products
{
    [Topic("ProductDomainEvent")]
    public class ProductCreated : AggregateRootEvent
    {
        public ProductCreated(Guid productId, string name, int count, DateTime createTime)
            : base(productId)
        {
            Name = name;
            Count = count;
            CreateTime = DateTime.Now;
        }

        public string Name { get; set; }
        public int Count { get; set; }
        public DateTime CreateTime { get; set; }
    }
}