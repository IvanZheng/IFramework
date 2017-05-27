using System;
using System.Runtime.Serialization;

namespace Kafka.Client.Exceptions
{
    public class IllegalStateException : Exception
    {
        public IllegalStateException()
        {
        }

        public IllegalStateException(string message)
            : base(message)
        {
        }

        public IllegalStateException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}