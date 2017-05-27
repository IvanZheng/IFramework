namespace Kafka.Client.Producers.Partitioning
{
    /// <summary>
    ///     User-defined partitioner
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    public interface IPartitioner<TKey>
    {
        /// <summary>
        ///     Uses the key to calculate a partition bucket id for routing
        ///     the data to the appropriate broker partition
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="numPartitions">The num partitions.</param>
        /// <returns>ID between 0 and numPartitions-1</returns>
        int Partition(TKey key, int numPartitions);
    }
}