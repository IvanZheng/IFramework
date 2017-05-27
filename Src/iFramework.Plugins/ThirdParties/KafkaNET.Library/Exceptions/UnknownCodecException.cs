using System;
using System.Runtime.Serialization;

namespace Kafka.Client.Exceptions
{
    public class UnknownCodecException : Exception
    {
        public UnknownCodecException()
        {
        }

        public UnknownCodecException(string message)
            : base(message)
        {
        }

        public UnknownCodecException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}