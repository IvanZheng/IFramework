namespace Kafka.Client.ZooKeeperIntegration.Events
{
    /// <summary>
    ///     Event types
    /// </summary>
    public enum ZooKeeperEventTypes
    {
        Unknow = 0,

        StateChanged = 1,

        SessionCreated = 2,

        ChildChanged = 3,

        DataChanged = 4
    }
}