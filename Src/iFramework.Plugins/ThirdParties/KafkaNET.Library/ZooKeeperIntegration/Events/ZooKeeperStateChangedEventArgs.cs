using ZooKeeperNet;

namespace Kafka.Client.ZooKeeperIntegration.Events
{
    /// <summary>
    ///     Contains ZooKeeper session state changed event data
    /// </summary>
    public class ZooKeeperStateChangedEventArgs : ZooKeeperEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ZooKeeperStateChangedEventArgs" /> class.
        /// </summary>
        /// <param name="state">
        ///     The current ZooKeeper state.
        /// </param>
        public ZooKeeperStateChangedEventArgs(KeeperState state)
            : base("State changed to " + state)
        {
            State = state;
        }

        /// <summary>
        ///     Gets current ZooKeeper state
        /// </summary>
        public KeeperState State { get; }

        /// <summary>
        ///     Gets the event type.
        /// </summary>
        public override ZooKeeperEventTypes Type => ZooKeeperEventTypes.StateChanged;
    }
}