using IFramework.Infrastructure.Logging;
using IFramework.IoC;

namespace Kafka.Client.ZooKeeperIntegration.Events
{
    /// <summary>
    ///     Represents methods that will handle a ZooKeeper data events
    /// </summary>
    internal class DataChangedEventItem
    {
        public static ILogger Logger = IoCFactory.Resolve<ILoggerFactory>().Create(typeof(DataChangedEventItem));

        private ZooKeeperClient.ZooKeeperEventHandler<ZooKeeperDataChangedEventArgs> dataChanged;
        private ZooKeeperClient.ZooKeeperEventHandler<ZooKeeperDataChangedEventArgs> dataDeleted;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DataChangedEventItem" /> class.
        /// </summary>
        /// <param name="logger">
        ///     The logger.
        /// </param>
        /// <remarks>
        ///     Should use external logger to keep same format of all event logs
        /// </remarks>
        public DataChangedEventItem()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="DataChangedEventItem" /> class.
        /// </summary>
        /// <param name="logger">
        ///     The logger.
        /// </param>
        /// <param name="changedHandler">
        ///     The changed handler.
        /// </param>
        /// <param name="deletedHandler">
        ///     The deleted handler.
        /// </param>
        /// <remarks>
        ///     Should use external logger to keep same format of all event logs
        /// </remarks>
        public DataChangedEventItem(ZooKeeperClient.ZooKeeperEventHandler<ZooKeeperDataChangedEventArgs> changedHandler,
            ZooKeeperClient.ZooKeeperEventHandler<ZooKeeperDataChangedEventArgs> deletedHandler)
        {
            DataChanged += changedHandler;
            DataDeleted += deletedHandler;
        }

        /// <summary>
        ///     Gets the total count of subscribed handlers
        /// </summary>
        public int TotalCount => (dataChanged != null ? dataChanged.GetInvocationList().Length : 0) +
                                 (dataDeleted != null ? dataDeleted.GetInvocationList().Length : 0);

        /// <summary>
        ///     Occurs when znode data changes
        /// </summary>
        public event ZooKeeperClient.ZooKeeperEventHandler<ZooKeeperDataChangedEventArgs> DataChanged
        {
            add
            {
                dataChanged -= value;
                dataChanged += value;
            }

            remove => dataChanged -= value;
        }

        /// <summary>
        ///     Occurs when znode data deletes
        /// </summary>
        public event ZooKeeperClient.ZooKeeperEventHandler<ZooKeeperDataChangedEventArgs> DataDeleted
        {
            add
            {
                dataDeleted -= value;
                dataDeleted += value;
            }

            remove => dataDeleted -= value;
        }

        /// <summary>
        ///     Invokes subscribed handlers for ZooKeeeper data changes event
        /// </summary>
        /// <param name="e">
        ///     The event data.
        /// </param>
        public void OnDataChanged(ZooKeeperDataChangedEventArgs e)
        {
            var handlers = dataChanged;
            if (handlers == null)
                return;

            foreach (var handler in handlers.GetInvocationList())
                Logger.Debug(e + " sent to " + handler.Target);

            handlers(e);
        }

        /// <summary>
        ///     Invokes subscribed handlers for ZooKeeeper data deletes event
        /// </summary>
        /// <param name="e">
        ///     The event data.
        /// </param>
        public void OnDataDeleted(ZooKeeperDataChangedEventArgs e)
        {
            var handlers = dataDeleted;
            if (handlers == null)
                return;

            foreach (var handler in handlers.GetInvocationList())
                Logger.Debug(e + " sent to " + handler.Target);

            handlers(e);
        }
    }
}