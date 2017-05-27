using System;
using System.Collections.Generic;
using System.Linq;
using IFramework.Infrastructure.Logging;
using IFramework.IoC;
using Kafka.Client.Cfg;
using Kafka.Client.Producers.Partitioning;
using Kafka.Client.Producers.Sync;
using Kafka.Client.Serialization;
using Kafka.Client.Utils;

namespace Kafka.Client.Producers
{
    /// <summary>
    ///     High-level Producer API that exposes all the producer functionality to the client
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TData">The type of the data.</typeparam>
    /// <remarks>
    ///     Provides serialization of data through a user-specified encoder, zookeeper based automatic broker discovery
    ///     and software load balancing through an optionally user-specified partitioner
    /// </remarks>
    public class Producer<TKey, TData> : KafkaClientBase, IProducer<TKey, TData>
    {
        public static ILogger Logger = IoCFactory.Resolve<ILoggerFactory>().Create("Producer");

        private readonly ICallbackHandler<TKey, TData> callbackHandler;
        private readonly object shuttingDownLock = new object();

        private readonly IDictionary<string, TopicMetadata> topicPartitionInfo = new Dictionary<string, TopicMetadata>()
            ;

        private readonly IDictionary<string, DateTime> topicPartitionInfoLastUpdateTime =
            new Dictionary<string, DateTime>();

        private volatile bool disposed;
        private readonly SyncProducerPool syncProducerPool;

        public Producer(ICallbackHandler<TKey, TData> callbackHandler)
        {
            this.callbackHandler = callbackHandler;
        }

        public Producer(ProducerConfiguration config)
        {
            Config = config;

            syncProducerPool = new SyncProducerPool(config);
            callbackHandler = new DefaultCallbackHandler<TKey, TData>(config,
                ReflectionHelper.Instantiate<IPartitioner<TKey>>(config.PartitionerClass),
                ReflectionHelper.Instantiate<IEncoder<TData>>(config.SerializerClass),
                new BrokerPartitionInfo(syncProducerPool, topicPartitionInfo, topicPartitionInfoLastUpdateTime,
                    Config.TopicMetaDataRefreshIntervalMS, syncProducerPool.zkClient),
                syncProducerPool);
        }

        public ProducerConfiguration Config { get; }

        /// <summary>
        ///     Sends the data to a multiple topics, partitioned by key
        /// </summary>
        /// <param name="data">The producer data objects that encapsulate the topic, key and message data.</param>
        public void Send(IEnumerable<ProducerData<TKey, TData>> data)
        {
            Guard.NotNull(data, "data");
            Guard.CheckBool(data.Any(), true, "data.Any()");

            EnsuresNotDisposed();

            callbackHandler.Handle(data);
        }

        /// <summary>
        ///     Sends the data to a single topic, partitioned by key, using either the
        ///     synchronous or the asynchronous producer.
        /// </summary>
        /// <param name="data">The producer data object that encapsulates the topic, key and message data.</param>
        public void Send(ProducerData<TKey, TData> data)
        {
            Guard.NotNull(data, "data");
            Guard.NotNullNorEmpty(data.Topic, "data.Topic");
            Guard.NotNull(data.Data, "data.Data");
            Guard.CheckBool(data.Data.Any(), true, "data.Data.Any()");

            EnsuresNotDisposed();

            Send(new List<ProducerData<TKey, TData>> {data});
        }

        public override string ToString()
        {
            if (Config == null)
                return "Producer: Config is null.";
            if (syncProducerPool == null)
                return "Producer: syncProducerPool is null.";
            return "Producer: " + syncProducerPool;
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            if (disposed)
                return;

            lock (shuttingDownLock)
            {
                if (disposed)
                    return;

                disposed = true;
            }

            try
            {
                if (callbackHandler != null)
                    callbackHandler.Dispose();
            }
            catch (Exception exc)
            {
                Logger.Error(exc.FormatException());
            }
        }

        /// <summary>
        ///     Ensures that object was not disposed
        /// </summary>
        private void EnsuresNotDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException(GetType().Name);
        }
    }
}