using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFramework.Infrastructure.Logging;
using IFramework.IoC;
using Kafka.Client.Cfg;
using Kafka.Client.Cluster;
using Kafka.Client.Exceptions;
using Kafka.Client.Utils;
using Kafka.Client.ZooKeeperIntegration;

namespace Kafka.Client.Producers.Sync
{
    /// <summary>
    ///     The base for all classes that represents pool of producers used by high-level API
    /// </summary>
    public class SyncProducerPool : ISyncProducerPool
    {
        public static ILogger Logger = IoCFactory.Resolve<ILoggerFactory>().Create(typeof(SyncProducerPool));
        private readonly ThreadSafeRandom random = new ThreadSafeRandom();

        /// <summary>
        ///     BrokerID  -->  SyncProducer
        /// </summary>
        internal readonly ConcurrentDictionary<int, SyncProducerWrapper> syncProducers;

        internal readonly ZooKeeperClient zkClient;

        public SyncProducerPool(ProducerConfiguration config)
        {
            syncProducers = new ConcurrentDictionary<int, SyncProducerWrapper>();
            Config = config;

            if (config.ZooKeeper != null)
            {
                zkClient = new ZooKeeperClient(config.ZooKeeper.ZkConnect, config.ZooKeeper.ZkSessionTimeoutMs,
                    ZooKeeperStringSerializer.Serializer);
                zkClient.Connect();
            }

            AddProducers(config);
        }

        public SyncProducerPool(ProducerConfiguration config, List<ISyncProducer> producers)
        {
            syncProducers = new ConcurrentDictionary<int, SyncProducerWrapper>();
            Config = config;

            if (config.ZooKeeper != null)
            {
                zkClient = new ZooKeeperClient(config.ZooKeeper.ZkConnect, config.ZooKeeper.ZkSessionTimeoutMs,
                    ZooKeeperStringSerializer.Serializer);
                zkClient.Connect();
            }

            if (producers != null && producers.Any())
                producers.ForEach(x => syncProducers.TryAdd(x.Config.BrokerId,
                    new SyncProducerWrapper(x, config.SyncProducerOfOneBroker)));
        }

        protected ProducerConfiguration Config { get; }

        protected bool Disposed { get; set; }

        public void AddProducer(Broker broker)
        {
            var syncProducerConfig = new SyncProducerConfiguration(Config, broker.Id, broker.Host, broker.Port);
            var producerWrapper = new SyncProducerWrapper(syncProducerConfig, Config.SyncProducerOfOneBroker);
            Logger.DebugFormat("Creating sync producer for broker id = {0} at {1}:{2} SyncProducerOfOneBroker:{3}",
                broker.Id, broker.Host, broker.Port, Config.SyncProducerOfOneBroker);
            syncProducers.TryAdd(broker.Id, producerWrapper);
        }

        public void AddProducers(ProducerConfiguration config)
        {
            var configuredBrokers = config.Brokers.Select(x => new Broker(x.BrokerId, x.Host, x.Port));
            if (configuredBrokers.Any())
            {
                configuredBrokers.ForEach(AddProducer);
            }
            else if (zkClient != null)
            {
                Logger.DebugFormat("Connecting to {0} for creating sync producers for all brokers in the cluster",
                    config.ZooKeeper.ZkConnect);
                var brokers = ZkUtils.GetAllBrokersInCluster(zkClient);
                brokers.ForEach(AddProducer);
            }
            else
            {
                throw new IllegalStateException("No producers found from configuration and zk not setup.");
            }
        }

        public List<ISyncProducer> GetShuffledProducers()
        {
            return syncProducers.Values.OrderBy(a => random.Next()).ToList().Select(r => r.GetProducer()).ToList();
        }

        public ISyncProducer GetProducer(int brokerId)
        {
            SyncProducerWrapper producerWrapper;
            syncProducers.TryGetValue(brokerId, out producerWrapper);
            if (producerWrapper == null)
                throw new UnavailableProducerException(
                    string.Format("Sync producer for broker id {0} does not exist", brokerId));

            return producerWrapper.GetProducer();
        }

        /// <summary>
        ///     Releases all unmanaged and managed resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("SyncProducerPool:");
            var configuredBrokers = Config.Brokers.Select(x => new Broker(x.BrokerId, x.Host, x.Port)).ToArray();
            sb.AppendFormat("\tSeed brokers:");
            sb.Append(string.Join(",", configuredBrokers.Select(r => r.ToString()).ToArray()));

            if (Config.ZooKeeper != null)
                sb.AppendFormat("\t Broker zookeeper: {0} \t", Config.ZooKeeper.ZkConnect);

            sb.Append(string.Join(",",
                syncProducers.Select(r => string.Format("BrokerID:{0} syncProducerCount:{1} ", r.Key,
                    r.Value.Producers.Count())).ToArray()));
            return sb.ToString();
        }

        public int Count()
        {
            return syncProducers.Count;
        }

        public List<ISyncProducer> GetProducers()
        {
            return syncProducers.OrderBy(a => a.Key).Select(r => r.Value).ToList().Select(r => r.GetProducer())
                .ToList();
        }

        protected void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            if (Disposed)
                return;

            Disposed = true;
            syncProducers.ForEach(x => x.Value.Dispose());
            if (zkClient != null)
                zkClient.Dispose();
        }

        internal class SyncProducerWrapper
        {
            private readonly object _lock = new object();
            public Queue<ISyncProducer> Producers;

            public SyncProducerWrapper(SyncProducerConfiguration syncProducerConfig, int count)
            {
                Producers = new Queue<ISyncProducer>(count);
                for (var i = 0; i < count; i++)
                    //TODO: if can't create , should retry later. should not block
                    Producers.Enqueue(new SyncProducer(syncProducerConfig));
            }

            public SyncProducerWrapper(ISyncProducer syncProducer, int count)
            {
                Producers = new Queue<ISyncProducer>(count);
                for (var i = 0; i < count; i++)
                    Producers.Enqueue(syncProducer);
            }

            protected bool Disposed { get; set; }

            public ISyncProducer GetProducer()
            {
                //round-robin instead of random peek, to avoid collision of producers.
                ISyncProducer producer = null;
                lock (_lock)
                {
                    producer = Producers.Peek();
                    Producers.Dequeue();
                    Producers.Enqueue(producer);
                }
                return producer;
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected void Dispose(bool disposing)
            {
                if (!disposing)
                    return;

                if (Disposed)
                    return;

                Disposed = true;
                Producers.ForEach(x => x.Dispose());
            }
        }
    }
}