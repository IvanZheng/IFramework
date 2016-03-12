using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Command
{
    public class GetProducts : CommandBase
    {
        public List<Guid> ProductIds { get; set; }
    }
}
