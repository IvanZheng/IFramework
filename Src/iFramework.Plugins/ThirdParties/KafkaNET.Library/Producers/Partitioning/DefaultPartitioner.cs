using System;
using System.Text;
using Kafka.Client.Utils;

namespace Kafka.Client.Producers.Partitioning
{
    /// <summary>
    ///     Default partitioner using hash code to calculate partition
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    public class DefaultPartitioner<TKey> : IPartitioner<TKey>
    {
        private static readonly Random Randomizer = new Random();

        /// <summary>
        ///     Uses the key to calculate a partition bucket id for routing
        ///     the data to the appropriate broker partition
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="numPartitions">The num partitions.</param>
        /// <returns>ID between 0 and numPartitions-1</returns>
        /// <remarks>
        ///     Uses hash code to calculate partition
        /// </remarks>
        public int Partition(TKey key, int numPartitions)
        {
            Guard.Greater(numPartitions, 0, "numPartitions");
            if (key.GetType() == typeof(byte[]))
                return key == null
                    ? Randomizer.Next(numPartitions)
                    : Abs(Encoding.UTF8.GetString((byte[]) Convert.ChangeType(key, typeof(byte[]))).GetHashCode()) %
                      numPartitions;

            return key == null
                ? Randomizer.Next(numPartitions)
                : Abs(key.GetHashCode()) % numPartitions;
        }

        private static int Abs(int n)
        {
            return n == int.MinValue ? 0 : Math.Abs(n);
        }
    }
}