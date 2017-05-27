using System;

namespace Kafka.Client.Exceptions
{
    /// <summary>
    ///     The exception that is thrown when could not retrieve offset from broker for specific partition of specific topic
    /// </summary>
    public class OffsetIsUnknowException : Exception
    {
        public OffsetIsUnknowException(string topic, int brokerId, int partitionId)
        {
            BrokerId = brokerId;
            PartitionId = partitionId;
            Topic = topic;
        }

        public OffsetIsUnknowException()
        {
        }

        public OffsetIsUnknowException(string message)
            : base(message)
        {
        }

        public int? BrokerId { get; set; }

        public int? PartitionId { get; set; }

        public string Topic { get; set; }
    }
}