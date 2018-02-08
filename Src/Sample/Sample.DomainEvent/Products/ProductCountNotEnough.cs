using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.DomainEvents.Products
{
    public class ProductCountNotEnough: AggregateRootExceptionEvent
    {
        public int ReduceCount { get; protected set; }
        public int Count { get; protected set; }

        public ProductCountNotEnough()
        {

        }
        public ProductCountNotEnough(Guid aggregateRootId, int reduceCount, int count)
            : base(aggregateRootId)
        {
            ReduceCount = reduceCount;
            Count = count;
        }

        public override string ToString()
        {
            return $"product({AggregateRootId}) ProductCountNotEnough count:{Count} reduceCount:{ReduceCount}";
        }
    }
}
