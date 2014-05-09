using IFramework.Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace IFramework.Domain
{
    public class TimestampedAggregateRoot : AggregateRoot
    {
        [Timestamp]
        public byte[] Version
        {
            get;
            private set;
        }
    }
}
