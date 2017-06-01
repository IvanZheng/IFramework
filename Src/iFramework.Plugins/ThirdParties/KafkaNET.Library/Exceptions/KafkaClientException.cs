using System;
using System.Runtime.Serialization;
using Kafka.Client.Utils;

namespace Kafka.Client.Exceptions
{
    public class KafkaClientException : Exception
    {
        public KafkaClientException() { }

        public KafkaClientException(string message) : base(message) { }

        public KafkaClientException(string message, ErrorMapping errorCode) : this(message)
        {
            ErrorCode = errorCode;
        }

        public KafkaClientException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }

        public ErrorMapping ErrorCode { get; set; }
    }
}