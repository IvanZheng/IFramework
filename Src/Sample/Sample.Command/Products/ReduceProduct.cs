using IFramework.Command.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Command
{
    public class ReduceProduct : LinearCommandBase
    {
        [LinerKey]
        public Guid ProductId { get; set; }
        public int ReduceCount { get; set; }
    }
}
