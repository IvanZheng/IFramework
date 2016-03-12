using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Command
{
    public class GetProduct : CommandBase
    {
        public Guid ProductId { get; set; }
    }
}
