using Kafka.Client.ZooKeeperIntegration.Events;

namespace Kafka.Client.ZooKeeperIntegration.Listeners
{
    /// <summary>
    ///     Listener that can be registered for listening on ZooKeeper znode data changes for a given path
    /// </summary>
    public interface IZooKeeperDataListener
    {
        /// <summary>
        ///     Called when the data of the given path changed
        /// </summary>
        /// <param name="args">
        ///     The <see cref="ZooKeeperDataChangedEventArgs" /> instance containing the event data
        ///     as path and data.
        /// </param>
        /// <remarks>
        ///     http://zookeeper.wiki.sourceforge.net/ZooKeeperWatches
        /// </remarks>
        void HandleDataChange(ZooKeeperDataChangedEventArgs args);

        /// <summary>
        ///     Called when the data of the given path was deleted
        /// </summary>
        /// <param name="args">
        ///     The <see cref="ZooKeeperDataChangedEventArgs" /> instance containing the event data
        ///     as path.
        /// </param>
        /// <remarks>
        ///     http://zookeeper.wiki.sourceforge.net/ZooKeeperWatches
        /// </remarks>
        void HandleDataDelete(ZooKeeperDataChangedEventArgs args);
    }
}