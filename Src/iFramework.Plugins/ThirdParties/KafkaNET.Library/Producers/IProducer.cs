using System;
using System.Collections.Generic;
using Kafka.Client.Cfg;

namespace Kafka.Client.Producers
{
    /// <summary>
    ///     High-level Producer API that exposing all the producer functionality through a single API to the client
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TData">The type of the data.</typeparam>
    public interface IProducer<TKey, TData> : IDisposable
    {
        ProducerConfiguration Config { get; }

        /// <summary>
        ///     Sends the data to a multiple topics, partitioned by key, using either the
        ///     synchronous or the asynchronous producer.
        /// </summary>
        /// <param name="data">The producer data objects that encapsulate the topic, key and message data.</param>
        void Send(IEnumerable<ProducerData<TKey, TData>> data);

        /// <summary>
        ///     Sends the data to a single topic, partitioned by key, using either the
        ///     synchronous or the asynchronous producer.
        /// </summary>
        /// <param name="data">The producer data object that encapsulates the topic, key and message data.</param>
        void Send(ProducerData<TKey, TData> data);
    }
}