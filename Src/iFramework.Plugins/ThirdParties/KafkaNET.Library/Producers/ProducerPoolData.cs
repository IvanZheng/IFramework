using System.Collections.Generic;
using Kafka.Client.Cluster;

namespace Kafka.Client.Producers
{
    /// <summary>
    ///     Encapsulates data to be send on chosen partition
    /// </summary>
    /// <typeparam name="TData">
    ///     Type of data
    /// </typeparam>
    internal class ProducerPoolData<TData>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ProducerPoolData{TData}" /> class.
        /// </summary>
        /// <param name="topic">
        ///     The topic.
        /// </param>
        /// <param name="bidPid">
        ///     The chosen partition.
        /// </param>
        /// <param name="data">
        ///     The data.
        /// </param>
        public ProducerPoolData(string topic, Partition bidPid, IEnumerable<TData> data)
        {
            Topic = topic;
            BidPid = bidPid;
            Data = data;
        }

        /// <summary>
        ///     Gets the topic.
        /// </summary>
        public string Topic { get; }

        /// <summary>
        ///     Gets the chosen partition.
        /// </summary>
        public Partition BidPid { get; }

        /// <summary>
        ///     Gets the data.
        /// </summary>
        public IEnumerable<TData> Data { get; }
    }
}