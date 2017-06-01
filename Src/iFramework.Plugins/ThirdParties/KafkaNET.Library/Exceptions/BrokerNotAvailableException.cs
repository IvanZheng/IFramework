using System;
using System.Runtime.Serialization;

namespace Kafka.Client.Exceptions
{
    /// <summary>
    ///     The exception that is thrown when broker information is not available on ZooKeeper server
    /// </summary>
    public class BrokerNotAvailableException : Exception
    {
        public BrokerNotAvailableException(int brokerId)
        {
            BrokerId = brokerId;
        }

        public BrokerNotAvailableException() { }

        public BrokerNotAvailableException(string message)
            : base(message) { }

        public BrokerNotAvailableException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }

        public int? BrokerId { get; set; }
    }
}