using System;
using System.Runtime.Serialization;

namespace Kafka.Client.Exceptions
{
    public class TimeStampTooSmallException : Exception
    {
        private long offsetTime;


        public TimeStampTooSmallException() { }

        public TimeStampTooSmallException(string message)
            : base(message) { }

        public TimeStampTooSmallException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }

        public TimeStampTooSmallException(long offsetTime)
        {
            this.offsetTime = offsetTime;
        }
    }
}