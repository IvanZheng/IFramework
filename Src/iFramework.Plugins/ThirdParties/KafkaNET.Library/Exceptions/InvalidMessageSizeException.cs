using System;
using System.Runtime.Serialization;

namespace Kafka.Client.Exceptions
{
    public class InvalidMessageSizeException : Exception
    {
        public InvalidMessageSizeException()
        {
        }

        public InvalidMessageSizeException(string message)
            : base(message)
        {
        }

        public InvalidMessageSizeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}