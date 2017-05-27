namespace Kafka.Client.Requests
{
    /// <summary>
    ///     Requests types for Kafka
    /// </summary>
    /// <remarks>
    ///     Many of these are not in play yet.
    ///     See https://cwiki.apache.org/confluence/display/KAFKA/A+Guide+To+The+Kafka+Protocol
    /// </remarks>
    public enum RequestTypes : short
    {
        /// <summary>
        ///     Produce a message.
        /// </summary>
        Produce = 0,

        /// <summary>
        ///     Fetch a message.
        /// </summary>
        Fetch = 1,

        /// <summary>
        ///     Gets offsets.
        /// </summary>
        Offsets = 2,

        /// <summary>
        ///     Gets topic metadata
        /// </summary>
        TopicMetadataRequest = 3,

        LeaderAndIsrRequest = 4,
        StopReplicaKey = 5,
        UpdateMetadataKey = 6,
        ControlledShutdownKey = 7
    }
}