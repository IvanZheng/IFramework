using System;
using System.Collections.Generic;
using System.Text;
using IFramework.Domain;

namespace IFramework.Test.EntityFramework
{
    public abstract class BaseEntity: AggregateRoot
    {
        public long Id { get; set; }

    }
}
