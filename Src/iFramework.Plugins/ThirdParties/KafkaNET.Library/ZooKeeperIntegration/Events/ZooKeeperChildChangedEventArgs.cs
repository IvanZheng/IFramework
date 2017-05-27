using System.Collections.Generic;

namespace Kafka.Client.ZooKeeperIntegration.Events
{
    /// <summary>
    ///     Contains znode children changed event data
    /// </summary>
    public class ZooKeeperChildChangedEventArgs : ZooKeeperEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ZooKeeperChildChangedEventArgs" /> class.
        /// </summary>
        /// <param name="path">
        ///     The path.
        /// </param>
        public ZooKeeperChildChangedEventArgs(string path)
            : base("Children of " + path + " changed")
        {
            Path = path;
        }

        /// <summary>
        ///     Gets the znode path
        /// </summary>
        public string Path { get; }

        /// <summary>
        ///     Gets or sets the current znode children
        /// </summary>
        public IEnumerable<string> Children { get; set; }

        /// <summary>
        ///     Gets the current event type
        /// </summary>
        public override ZooKeeperEventTypes Type => ZooKeeperEventTypes.ChildChanged;
    }
}