using System;
using System.Runtime.Serialization;

namespace Kafka.Client.Exceptions
{
    public class NoLeaderForPartitionException : Exception
    {
        public NoLeaderForPartitionException()
        {
        }

        public NoLeaderForPartitionException(int partitionId)
        {
            PartitionId = partitionId;
        }

        public NoLeaderForPartitionException(string message)
            : base(message)
        {
        }

        public NoLeaderForPartitionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public int? PartitionId { get; set; }
    }
}