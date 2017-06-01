using System;
using System.Runtime.Serialization;

namespace Kafka.Client.Exceptions
{
    public class IllegalQueueStateException : Exception
    {
        public IllegalQueueStateException() { }

        public IllegalQueueStateException(string message)
            : base(message) { }

        public IllegalQueueStateException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}