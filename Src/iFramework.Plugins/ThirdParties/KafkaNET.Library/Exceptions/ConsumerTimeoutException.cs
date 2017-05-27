using System;
using System.Runtime.Serialization;

namespace Kafka.Client.Exceptions
{
    public class ConsumerTimeoutException : Exception
    {
        public ConsumerTimeoutException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public ConsumerTimeoutException()
        {
        } // Only used by Orleans runtime
    }
}