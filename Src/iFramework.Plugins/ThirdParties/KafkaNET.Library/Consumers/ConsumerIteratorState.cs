namespace Kafka.Client.Consumers
{
    internal enum ConsumerIteratorState
    {
        Done,
        Ready,
        NotReady,
        Failed
    }
}