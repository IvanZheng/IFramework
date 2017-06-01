using System;
using System.Runtime.Serialization;

namespace Kafka.Client.Exceptions
{
    public class ZooKeeperTimeoutException : Exception
    {
        public ZooKeeperTimeoutException()
            : base("Unable to connect to zookeeper server within timeout: unknown value") { }

        public ZooKeeperTimeoutException(int connectionTimeout)
            : base("Unable to connect to zookeeper server within timeout: " + connectionTimeout) { }

        public ZooKeeperTimeoutException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}