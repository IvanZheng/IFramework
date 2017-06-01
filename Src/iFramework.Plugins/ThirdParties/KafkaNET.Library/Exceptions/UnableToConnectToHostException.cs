using System;
using System.Runtime.Serialization;

namespace Kafka.Client.Exceptions
{
    public class UnableToConnectToHostException : Exception
    {
        public UnableToConnectToHostException(string server, int port)
            : base(string.Format("Unable to connect to {0}:{1}", server, port)) { }

        public UnableToConnectToHostException(string server, int port, Exception innterException)
            : base(string.Format("Unable to connect to {0}:{1}", server, port), innterException) { }

        public UnableToConnectToHostException() { }

        public UnableToConnectToHostException(string message, Exception innterException)
            : base(message, innterException) { }

        public UnableToConnectToHostException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}