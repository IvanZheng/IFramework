using System.ComponentModel.DataAnnotations;

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
        { get; private set; }
    }
}