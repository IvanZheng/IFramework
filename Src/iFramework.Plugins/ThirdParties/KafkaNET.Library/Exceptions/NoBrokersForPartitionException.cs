using System;
using System.Runtime.Serialization;

namespace Kafka.Client.Exceptions
{
    public class NoBrokersForPartitionException : Exception
    {
        public NoBrokersForPartitionException()
        {
        }

        public NoBrokersForPartitionException(string message)
            : base(message)
        {
        }

        public NoBrokersForPartitionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}