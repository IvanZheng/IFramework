using System.Collections.Generic;

namespace Kafka.Client.Consumers
{
    /// <summary>
    ///     Represent current state of consuming specified topic
    /// </summary>
    public class TopicStatisticsRecord
    {
        /// <summary>
        ///     Gets or sets topic name
        /// </summary>
        public string Topic { get; set; }

        /// <summary>
        ///     Gets or sets statistics for each partition within a topic
        /// </summary>
        public IDictionary<int, PartitionStatisticsRecord> PartitionsStat { get; set; }

        /// <summary>
        ///     Gets the total number of messages in all topics that were not consumed yet.
        /// </summary>
        public long Lag
        {
            get
            {
                if (PartitionsStat == null)
                    return 0;

                long result = 0;
                foreach (var partitionStatRecord in PartitionsStat.Values)
                    result += partitionStatRecord.Lag;

                return result;
            }
        }
    }
}