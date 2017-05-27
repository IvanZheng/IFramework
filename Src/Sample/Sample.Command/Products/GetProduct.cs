using System;
using System.Collections.Generic;

namespace Sample.Command
{
    public class GetProducts : CommandBase
    {
        public List<Guid> ProductIds { get; set; }
    }
}