using System;
using System.Collections.Generic;
using Kafka.Client.Cfg;
using Kafka.Client.Cluster;
using Kafka.Client.Producers.Sync;

namespace Kafka.Client.Producers
{
    /// <summary>
    ///     Pool of producers used by producer high-level API
    /// </summary>
    public interface ISyncProducerPool : IDisposable
    {
        /// <summary>
        ///     Add a new producer, either synchronous or asynchronous, to the pool
        /// </summary>
        /// <param name="broker">The broker informations.</param>
        void AddProducer(Broker broker);

        List<ISyncProducer> GetShuffledProducers();
        void AddProducers(ProducerConfiguration producerConfig);
        ISyncProducer GetProducer(int brokerId);
    }
}