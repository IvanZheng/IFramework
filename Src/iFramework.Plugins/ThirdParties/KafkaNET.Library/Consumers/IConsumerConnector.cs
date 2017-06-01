using System;
using System.Collections.Generic;
using Kafka.Client.Serialization;

namespace Kafka.Client.Consumers
{
    /// <summary>
    ///     The consumer high-level API, that hides the details of brokers from the consumer
    ///     It also maintains the state of what has been consumed.
    /// </summary>
    public interface IConsumerConnector : IDisposable
    {
        /// <summary>
        ///     Creates a list of message streams for each topic.
        /// </summary>
        /// <param name="topicCountDict">
        ///     The map of topic on number of streams
        /// </param>
        /// <returns>
        ///     The list of <see cref="KafkaMessageStream" />, which are iterators over topic.
        /// </returns>
        IDictionary<string, IList<KafkaMessageStream<TData>>> CreateMessageStreams<TData>(
            IDictionary<string, int> topicCountDict,
            IDecoder<TData> decoder);

        /// <summary>
        ///     Commits the offsets of all messages consumed so far.
        /// </summary>
        void CommitOffsets();

        /// <summary>
        ///     Do manually commit offset.  When use this API, The AutoCommit should be false.
        ///     Use it when process time of message are very vary. For example,
        ///     Read message with offset 0,1,2,3,4,5,6,7,8...
        ///     But message 3 process take very long time.
        ///     Firstly 0,1,2 has been processed, you can commit offset 2
        ///     Then 4,5,6 has been processed, do not commit offset
        ///     Then 3 has been processed, then you can directly commit offset 6.
        ///     Potential risk is that, before you commit 3, the running process crashed (for exmaple, autopilog IMP), then after
        ///     restart
        ///     Message 3,4,5,6 will be Reprocessed.
        /// </summary>
        /// <param name="topic">The topic </param>
        /// <param name="partition">The partition</param>
        /// <param name="offset">The offset</param>
        /// <param name="setPosition">Indicates whether to set the fetcher's offset to the value committed. Default = true.</param>
        void CommitOffset(string topic, int partition, long offset, bool setPosition = true);

        /// <summary>
        ///     Return offsets of current ConsumerGroup
        /// </summary>
        /// <param name="topic"></param>
        /// <returns></returns>
        Dictionary<int, long> GetOffset(string topic);
    }
}