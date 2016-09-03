using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.DomainEvents.Products
{
    public class ProductCreated : DomainEvent
    {
        public string Name { get; set; }
        public int Count { get; set; }
        public DateTime CreateTime { get; set; }

        public ProductCreated(Guid productId, string name, int count, DateTime createTime)
            : base(productId)
        {
            Name = name;
            Count = count;
            CreateTime = DateTime.Now;
        }
    }
}
