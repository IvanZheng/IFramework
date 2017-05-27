using System;
using System.Runtime.Serialization;

namespace Kafka.Client.Exceptions
{
    public class ZKRebalancerException : Exception
    {
        public ZKRebalancerException()
        {
        }

        public ZKRebalancerException(string message)
            : base(message)
        {
        }

        public ZKRebalancerException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}