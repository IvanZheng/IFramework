using System;
using System.Runtime.Serialization;

namespace Kafka.Client.Exceptions
{
    public class NoLoaderForPartitionException : Exception
    {
        public NoLoaderForPartitionException() { }

        public NoLoaderForPartitionException(string message)
            : base(message) { }

        public NoLoaderForPartitionException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}