using System;

namespace Kafka.Client.Exceptions
{
    /// <summary>
    ///     The exception that is thrown when no partitions found for specified topic
    /// </summary>
    public class NoPartitionsForTopicException : Exception
    {
        public NoPartitionsForTopicException(string topic)
        {
            Topic = topic;
        }

        public NoPartitionsForTopicException()
        {
        }

        public NoPartitionsForTopicException(string topic, string message)
            : base(message)
        {
            Topic = topic;
        }

        public string Topic { get; set; }
    }
}