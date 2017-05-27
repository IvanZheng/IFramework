using System;

namespace Kafka.Client.ZooKeeperIntegration.Events
{
    /// <summary>
    ///     Base class for classes containing ZooKeeper event data
    /// </summary>
    public abstract class ZooKeeperEventArgs : EventArgs
    {
        private readonly string description;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ZooKeeperEventArgs" /> class.
        /// </summary>
        /// <param name="description">
        ///     The event description.
        /// </param>
        protected ZooKeeperEventArgs(string description)
        {
            this.description = description;
        }

        /// <summary>
        ///     Gets the event type.
        /// </summary>
        public abstract ZooKeeperEventTypes Type { get; }

        /// <summary>
        ///     Gets string representation of event data
        /// </summary>
        /// <returns>
        ///     String representation of event data
        /// </returns>
        public override string ToString()
        {
            return "ZooKeeperEvent[" + description + "]";
        }
    }
}