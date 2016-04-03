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
#if mysql
        public DateTime Version
#else
        public byte[] Version
#endif
        {
            get;
            private set;
        }


    }

}
