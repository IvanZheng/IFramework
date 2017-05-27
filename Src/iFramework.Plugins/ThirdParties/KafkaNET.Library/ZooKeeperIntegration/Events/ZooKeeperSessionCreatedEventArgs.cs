namespace Kafka.Client.ZooKeeperIntegration.Events
{
    /// <summary>
    ///     Contains ZooKeeper session created event data
    /// </summary>
    public class ZooKeeperSessionCreatedEventArgs : ZooKeeperEventArgs
    {
        public new static readonly ZooKeeperSessionCreatedEventArgs Empty = new ZooKeeperSessionCreatedEventArgs();

        /// <summary>
        ///     Initializes a new instance of the <see cref="ZooKeeperSessionCreatedEventArgs" /> class.
        /// </summary>
        protected ZooKeeperSessionCreatedEventArgs()
            : base("New session created")
        {
        }

        /// <summary>
        ///     Gets the event type.
        /// </summary>
        public override ZooKeeperEventTypes Type => ZooKeeperEventTypes.SessionCreated;
    }
}