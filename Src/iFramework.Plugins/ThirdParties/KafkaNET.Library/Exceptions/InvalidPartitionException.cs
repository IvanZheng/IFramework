using System;
using System.Runtime.Serialization;

namespace Kafka.Client.Exceptions
{
    public class InvalidPartitionException : Exception
    {
        public InvalidPartitionException() { }

        public InvalidPartitionException(string message)
            : base(message) { }

        public InvalidPartitionException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}