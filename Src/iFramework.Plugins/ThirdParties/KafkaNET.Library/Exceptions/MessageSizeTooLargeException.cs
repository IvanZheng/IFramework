using System;
using System.Runtime.Serialization;

namespace Kafka.Client.Exceptions
{
    public class MessageSizeTooLargeException : Exception
    {
        public MessageSizeTooLargeException()
        {
        }

        public MessageSizeTooLargeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}