namespace Kafka.Client.Consumers
{
    public enum KafkaOffsetType
    {
        Earliest = 0,
        Current = 1,
        Last = 2,
        Latest = 3,
        Timestamp = 4
    }
}