using Kafka.Client.ZooKeeperIntegration.Events;

namespace Kafka.Client.ZooKeeperIntegration.Listeners
{
    /// <summary>
    ///     Listener that can be registered for listening on ZooKeeper znode changes for a given path
    /// </summary>
    public interface IZooKeeperChildListener
    {
        /// <summary>
        ///     Called when the children of the given path changed
        /// </summary>
        /// <param name="args">
        ///     The <see cref="Kafka.Client.ZooKeeperIntegration.Events.ZooKeeperChildChangedEventArgs" /> instance containing the
        ///     event data
        ///     as parent path and children (null if parent was deleted).
        /// </param>
        /// <remarks>
        ///     http://zookeeper.wiki.sourceforge.net/ZooKeeperWatches
        /// </remarks>
        void HandleChildChange(ZooKeeperChildChangedEventArgs args);

        /// <summary>
        ///     Resets the state of listener.
        /// </summary>
        void ResetState();
    }
}