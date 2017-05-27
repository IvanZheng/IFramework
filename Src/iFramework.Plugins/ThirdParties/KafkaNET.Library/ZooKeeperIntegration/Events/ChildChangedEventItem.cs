using IFramework.Infrastructure.Logging;
using IFramework.IoC;

namespace Kafka.Client.ZooKeeperIntegration.Events
{
    /// <summary>
    ///     Represents methods that will handle a ZooKeeper child events
    /// </summary>
    internal class ChildChangedEventItem
    {
        public static ILogger Logger = IoCFactory.Resolve<ILoggerFactory>().Create(typeof(ChildChangedEventItem));

        private ZooKeeperClient.ZooKeeperEventHandler<ZooKeeperChildChangedEventArgs> childChanged;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ChildChangedEventItem" /> class.
        /// </summary>
        /// <param name="logger">
        ///     The logger.
        /// </param>
        /// <remarks>
        ///     Should use external logger to keep same format of all event logs
        /// </remarks>
        public ChildChangedEventItem()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ChildChangedEventItem" /> class.
        /// </summary>
        /// <param name="logger">
        ///     The logger.
        /// </param>
        /// <param name="handler">
        ///     The subscribed handler.
        /// </param>
        /// <remarks>
        ///     Should use external logger to keep same format of all event logs
        /// </remarks>
        public ChildChangedEventItem(ZooKeeperClient.ZooKeeperEventHandler<ZooKeeperChildChangedEventArgs> handler)
        {
            ChildChanged += handler;
        }

        /// <summary>
        ///     Gets the total count of subscribed handlers
        /// </summary>
        public int Count => childChanged != null ? childChanged.GetInvocationList().Length : 0;

        /// <summary>
        ///     Occurs when znode children changes
        /// </summary>
        public event ZooKeeperClient.ZooKeeperEventHandler<ZooKeeperChildChangedEventArgs> ChildChanged
        {
            add
            {
                childChanged -= value;
                childChanged += value;
            }
            remove => childChanged -= value;
        }

        /// <summary>
        ///     Invokes subscribed handlers for ZooKeeeper children changes event
        /// </summary>
        /// <param name="e">
        ///     The event data.
        /// </param>
        public void OnChildChanged(ZooKeeperChildChangedEventArgs e)
        {
            var handlers = childChanged;
            if (handlers == null)
                return;

            foreach (var handler in handlers.GetInvocationList())
                Logger.Debug(e + " sent to " + handler.Target);

            handlers(e);
        }
    }
}