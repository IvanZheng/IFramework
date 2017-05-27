using System;
using System.Globalization;

namespace Kafka.Client.Utils
{
    /// <summary>
    ///     https://cwiki.apache.org/confluence/display/KAFKA/A+Guide+To+The+Kafka+Protocol
    /// </summary>
    public enum ErrorMapping : short
    {
        UnknownCode = -1,
        NoError = 0,
        OffsetOutOfRangeCode = 1,
        InvalidMessageCode = 2,
        UnknownTopicOrPartitionCode = 3,
        InvalidFetchSizeCode = 4,
        LeaderNotAvailableCode = 5,
        NotLeaderForPartitionCode = 6,
        RequestTimedOutCode = 7,
        BrokerNotAvailableCode = 8,
        ReplicaNotAvailableCode = 9,
        MessagesizeTooLargeCode = 10,
        StaleControllerEpochCode = 11,
        OffsetMetadataTooLargeCode = 12,
        StaleLeaderEpochCode = 13,
        OffsetsLoadInProgressCode = 14,
        ConsumerCoordinatorNotAvailableCode = 15,
        NotCoordinatorForConsumerCode = 16
    }

    public static class ErrorMapper
    {
        public static ErrorMapping ToError(short error)
        {
            return (ErrorMapping) Enum.Parse(typeof(ErrorMapping), error.ToString(CultureInfo.InvariantCulture));
        }
    }
}