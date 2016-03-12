using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Command
{
    public class CreateProduct : LinearCommandBase
    {
        public Guid ProductId { get; set; }
        public string Name { get; set; }
        public int Count { get; set; }
    }
}
