using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.Domain
{
    public class Entity : IEntity
    {
        [Newtonsoft.Json.JsonIgnore]
        public object DomainContext { get; set; }
    }
}
