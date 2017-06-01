namespace Kafka.Client.ZooKeeperIntegration.Events
{
    /// <summary>
    ///     Contains znode data changed event data
    /// </summary>
    public class ZooKeeperDataChangedEventArgs : ZooKeeperEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ZooKeeperDataChangedEventArgs" /> class.
        /// </summary>
        /// <param name="path">
        ///     The znode path.
        /// </param>
        public ZooKeeperDataChangedEventArgs(string path)
            : base("Data of " + path + " changed")
        {
            Path = path;
        }

        /// <summary>
        ///     Gets the znode path
        /// </summary>
        public string Path { get; }

        /// <summary>
        ///     Gets or sets znode changed data.
        /// </summary>
        /// <remarks>
        ///     Null if data was deleted.
        /// </remarks>
        public string Data { get; set; }

        /// <summary>
        ///     Gets the event type.
        /// </summary>
        public override ZooKeeperEventTypes Type => ZooKeeperEventTypes.DataChanged;

        /// <summary>
        ///     Gets a value indicating whether data was deleted
        /// </summary>
        public bool DataDeleted => string.IsNullOrEmpty(Data);

        /// <summary>
        ///     Gets string representation of event data
        /// </summary>
        /// <returns>
        ///     String representation of event data
        /// </returns>
        public override string ToString()
        {
            if (DataDeleted)
            {
                return base.ToString().Replace("changed", "deleted");
            }

            return base.ToString();
        }
    }
}