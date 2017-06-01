using System;
using System.Runtime.Serialization;

namespace Kafka.Client.Exceptions
{
    public class BadMessageSizeFromKafkaException : Exception
    {
        public BadMessageSizeFromKafkaException(int expected, int found) :
            base(string.Format("Message size expected to be {0} but read {1}", expected, found)) { }

        public BadMessageSizeFromKafkaException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }

        public BadMessageSizeFromKafkaException() { }
    }
}