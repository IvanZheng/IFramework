using IFramework.Domain;
using Sample.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Domain.Model
{
    public class Product : TimestampedAggregateRoot
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int Count { get; set; }
        public DateTime CreateTime { get; set; }
        public Product() { }
        public Product(Guid id, string name, int count)
        {
            Id = id;
            Name = name;
            Count = count;
            CreateTime = DateTime.Now;
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
                throw new IFramework.SysExceptions.SysException(ErrorCode.CountNotEnougth);
            }
        }
    }
}
