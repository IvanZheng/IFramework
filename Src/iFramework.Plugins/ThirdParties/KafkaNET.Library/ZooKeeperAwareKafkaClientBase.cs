using Kafka.Client.Cfg;

namespace Kafka.Client
{
    /// <summary>
    ///     A base class for all Kafka clients that support ZooKeeper based automatic broker discovery
    /// </summary>
    public abstract class ZooKeeperAwareKafkaClientBase : KafkaClientBase
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ZooKeeperAwareKafkaClientBase" /> class.
        /// </summary>
        /// <param name="config">The config.</param>
        protected ZooKeeperAwareKafkaClientBase(ZooKeeperConfiguration config)
        {
            IsZooKeeperEnabled = config != null && !string.IsNullOrEmpty(config.ZkConnect);
        }

        /// <summary>
        ///     Gets a value indicating whether ZooKeeper based automatic broker discovery is enabled.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this instance is zoo keeper enabled; otherwise, <c>false</c>.
        /// </value>
        protected bool IsZooKeeperEnabled { get; }
    }
}