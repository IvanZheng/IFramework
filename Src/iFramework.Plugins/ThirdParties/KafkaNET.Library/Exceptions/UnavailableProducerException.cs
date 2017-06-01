using System;
using System.Runtime.Serialization;

namespace Kafka.Client.Exceptions
{
    public class UnavailableProducerException : Exception
    {
        public UnavailableProducerException() { }

        public UnavailableProducerException(string message)
            : base(message) { }

        public UnavailableProducerException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}