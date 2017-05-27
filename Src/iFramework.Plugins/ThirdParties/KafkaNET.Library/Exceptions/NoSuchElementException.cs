using System;
using System.Runtime.Serialization;

namespace Kafka.Client.Exceptions
{
    public class NoSuchElementException : Exception
    {
        public NoSuchElementException()
        {
        }

        public NoSuchElementException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}