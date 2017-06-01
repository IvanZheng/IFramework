using System;
using System.Runtime.Serialization;

namespace Kafka.Client.Exceptions
{
    public class InvalidBrokerIdException : Exception
    {
        public InvalidBrokerIdException() { }

        public InvalidBrokerIdException(string message)
            : base(message) { }

        public InvalidBrokerIdException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}