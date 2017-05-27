using System;
using System.Collections.Generic;
using Kafka.Client.Cfg;
using Kafka.Client.Requests;
using Kafka.Client.Responses;

namespace Kafka.Client.Producers.Sync
{
    /// <summary>
    ///     Sends messages encapsulated in request to Kafka server synchronously
    /// </summary>
    public interface ISyncProducer : IDisposable
    {
        SyncProducerConfiguration Config { get; }

        /// <summary>
        ///     Sends a producer request to Kafka server synchronously
        /// </summary>
        /// <param name="request">
        ///     The request.
        /// </param>
        ProducerResponse Send(ProducerRequest request);

        /// <summary>
        ///     Sends a topic metadata request to Kafka server synchronously
        /// </summary>
        /// <param name="request">The Request</param>
        /// <returns>The Response</returns>
        IEnumerable<TopicMetadata> Send(TopicMetadataRequest request);
    }
}