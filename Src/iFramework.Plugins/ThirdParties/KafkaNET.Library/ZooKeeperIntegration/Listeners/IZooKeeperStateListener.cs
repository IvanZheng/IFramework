using Kafka.Client.ZooKeeperIntegration.Events;

namespace Kafka.Client.ZooKeeperIntegration.Listeners
{
    /// <summary>
    ///     Handles the session expiration event in ZooKeeper
    /// </summary>
    public interface IZooKeeperStateListener
    {
        /// <summary>
        ///     Called when the ZooKeeper connection state has changed.
        /// </summary>
        /// <param name="args">
        ///     The <see cref="Kafka.Client.ZooKeeperIntegration.Events.ZooKeeperStateChangedEventArgs" /> instance
        ///     containing the event data.
        /// </param>
        void HandleStateChanged(ZooKeeperStateChangedEventArgs args);

        /// <summary>
        ///     Called after the ZooKeeper session has expired and a new session has been created.
        /// </summary>
        /// <param name="args">
        ///     The <see cref="Kafka.Client.ZooKeeperIntegration.Events.ZooKeeperSessionCreatedEventArgs" />
        ///     instance containing the event data.
        /// </param>
        /// <remarks>
        ///     You would have to re-create any ephemeral nodes here.
        /// </remarks>
        void HandleSessionCreated(ZooKeeperSessionCreatedEventArgs args);
    }
}