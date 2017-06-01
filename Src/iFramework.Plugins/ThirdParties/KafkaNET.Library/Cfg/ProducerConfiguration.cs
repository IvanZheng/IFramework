using System.Collections.Generic;
using Kafka.Client.Messages;

namespace Kafka.Client.Cfg
{
    /// <summary>
    ///     High-level API configuration for the producer
    /// </summary>
    public class ProducerConfiguration : ISyncProducerConfigShared
    {
        public const string DefaultPartitioner = "Kafka.Client.Producers.Partitioning.DefaultPartitioner`1";
        public const string DefaultSerializer = "Kafka.Client.Serialization.DefaultEncoder";
        public const string DefaultSectionName = "kafkaProducer";
        public const int DefaultProducerRetries = 3;
        public const int DefaultProducerRetryBackoffMinMs = 100;
        public const int DefaultProducerRetryBackoffMaxMs = 1000;
        public const int DefaultTopicMetaDataRefreshIntervalMS = 600 * 1000;
        public const int DefaultSyncProducerOfOneBroker = 16;

        private string partitionerClass;

        private string serializerClass;

        private ProducerConfiguration()
        {
            BufferSize = SyncProducerConfiguration.DefaultBufferSize;
            ConnectTimeout = SyncProducerConfiguration.DefaultConnectTimeout;
            ReceiveTimeout = SyncProducerConfiguration.DefaultReceiveTimeout;
            SendTimeout = SyncProducerConfiguration.DefaultSendTimeout;
            ReconnectInterval = SyncProducerConfiguration.DefaultReconnectInterval;
            MaxMessageSize = SyncProducerConfiguration.DefaultMaxMessageSize;
            //TODO: Why it's default compressionCodec?  It will got to gzip.
            CompressionCodec = CompressionCodecs.DefaultCompressionCodec;
            CompressedTopics = new List<string>();
            ProducerRetries = DefaultProducerRetries;
            ProducerRetryExponentialBackoffMinMs = DefaultProducerRetryBackoffMinMs;
            ProducerRetryExponentialBackoffMaxMs = DefaultProducerRetryBackoffMaxMs;
            ClientId = SyncProducerConfiguration.DefaultClientId;
            RequiredAcks = SyncProducerConfiguration.DefaultRequiredAcks;
            AckTimeout = SyncProducerConfiguration.DefaultAckTimeout;

            // if TotalNumPartitions is not set (initialized to 0), the Producer will use the ProductData.Key
            // and the number of partitions retrieved from ProducerConf.Brokers, to determine which partition
            // to produce the data to.
            TotalNumPartitions = 0;
            TopicMetaDataRefreshIntervalMS = DefaultTopicMetaDataRefreshIntervalMS;
            SyncProducerOfOneBroker = DefaultSyncProducerOfOneBroker;
            ForceToPartition = -1;
            Verbose = false;
            Brokers = new List<BrokerConfiguration>();
        }

        public ProducerConfiguration(IList<BrokerConfiguration> brokersConfig)
            : this()
        {
            Brokers = brokersConfig;
        }

        public ProducerConfiguration(ProducerConfiguration producerConfigTemplate,
                                     List<BrokerConfiguration> brokersConfig,
                                     int partitionID)
        {
            Brokers = brokersConfig;
            ForceToPartition = partitionID;
            SyncProducerOfOneBroker = producerConfigTemplate.SyncProducerOfOneBroker;

            BufferSize = producerConfigTemplate.BufferSize;
            ConnectTimeout = producerConfigTemplate.ConnectTimeout;
            ReceiveTimeout = producerConfigTemplate.ReceiveTimeout;
            SendTimeout = producerConfigTemplate.SendTimeout;
            ReconnectInterval = producerConfigTemplate.ReconnectInterval;
            MaxMessageSize = producerConfigTemplate.MaxMessageSize;
            CompressionCodec = producerConfigTemplate.CompressionCodec;
            CompressedTopics = producerConfigTemplate.CompressedTopics;
            ProducerRetries = producerConfigTemplate.ProducerRetries;
            ProducerRetryExponentialBackoffMinMs = producerConfigTemplate.ProducerRetryExponentialBackoffMinMs;
            ProducerRetryExponentialBackoffMaxMs = producerConfigTemplate.ProducerRetryExponentialBackoffMaxMs;
            ClientId = producerConfigTemplate.ClientId;
            RequiredAcks = producerConfigTemplate.RequiredAcks;
            AckTimeout = producerConfigTemplate.AckTimeout;
            TotalNumPartitions = producerConfigTemplate.TotalNumPartitions;
            TopicMetaDataRefreshIntervalMS = producerConfigTemplate.TopicMetaDataRefreshIntervalMS;
            Verbose = producerConfigTemplate.Verbose;
        }

        public ProducerConfiguration(ProducerConfiguration producerConfigTemplate)
        {
            ForceToPartition = -1;
            SyncProducerOfOneBroker = producerConfigTemplate.SyncProducerOfOneBroker;

            BufferSize = producerConfigTemplate.BufferSize;
            ConnectTimeout = producerConfigTemplate.ConnectTimeout;
            ReceiveTimeout = producerConfigTemplate.ReceiveTimeout;
            SendTimeout = producerConfigTemplate.SendTimeout;
            ReconnectInterval = producerConfigTemplate.ReconnectInterval;
            MaxMessageSize = producerConfigTemplate.MaxMessageSize;
            CompressionCodec = producerConfigTemplate.CompressionCodec;
            CompressedTopics = producerConfigTemplate.CompressedTopics;
            ProducerRetries = producerConfigTemplate.ProducerRetries;
            ProducerRetryExponentialBackoffMinMs = producerConfigTemplate.ProducerRetryExponentialBackoffMinMs;
            ProducerRetryExponentialBackoffMaxMs = producerConfigTemplate.ProducerRetryExponentialBackoffMaxMs;
            ClientId = producerConfigTemplate.ClientId;
            RequiredAcks = producerConfigTemplate.RequiredAcks;
            AckTimeout = producerConfigTemplate.AckTimeout;
            TotalNumPartitions = producerConfigTemplate.TotalNumPartitions;
            TopicMetaDataRefreshIntervalMS = producerConfigTemplate.TopicMetaDataRefreshIntervalMS;
            Verbose = producerConfigTemplate.Verbose;
            partitionerClass = producerConfigTemplate.PartitionerClass;
            Brokers = new List<BrokerConfiguration>();
        }

        /// <summary>
        ///     Gets a value indicating whether ZooKeeper based automatic broker discovery is enabled.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this instance is zoo keeper enabled; otherwise, <c>false</c>.
        /// </value>
        public bool IsZooKeeperEnabled => ZooKeeper != null;

        public IList<BrokerConfiguration> Brokers { get; set; }

        public ZooKeeperConfiguration ZooKeeper { get; set; }

        public string PartitionerClass
        {
            get
            {
                if (string.IsNullOrEmpty(partitionerClass))
                {
                    return DefaultPartitioner;
                }

                return partitionerClass;
            }

            set => partitionerClass = value;
        }

        public short VersionId { get; set; }

        public string SerializerClass
        {
            get
            {
                if (string.IsNullOrEmpty(serializerClass))
                {
                    return DefaultSerializer;
                }

                return serializerClass;
            }
            set => serializerClass = value;
        }

        //public string CallbackHandlerClass { get; set; }

        public CompressionCodecs CompressionCodec { get; set; }

        public IEnumerable<string> CompressedTopics { get; set; }

        public int ProducerRetries { get; set; }

        public int ProducerRetryExponentialBackoffMinMs { get; set; }
        public int ProducerRetryExponentialBackoffMaxMs { get; set; }

        //public int QueueTime { get; set; }

        //public int QueueSize { get; set; }

        //public int BatchSize { get; set; }

        //public int EnqueueTimeoutMs { get; set; }

        /// <summary>
        ///     Total partition number of topic. Default value 0.
        ///     if not set, will take the partitions which hav metadata available.
        ///     Please ALWAYS keep this number as 0.
        /// </summary>
        public int TotalNumPartitions { get; set; }

        //topic.metadata.refresh.interval.ms
        public int TopicMetaDataRefreshIntervalMS { get; set; }

        public int SyncProducerOfOneBroker { get; set; }

        public int ForceToPartition { get; set; }

        public bool Verbose { get; set; }

        public int BufferSize { get; set; }

        public int ConnectTimeout { get; set; }

        public int ReconnectInterval { get; set; }


        /// <summary>
        ///     Socket recieve timeout , defautl 5000 ms.
        /// </summary>
        public int ReceiveTimeout { get; set; }

        /// <summary>
        ///     Socket send timeout , default 5000 ms.
        /// </summary>
        public int SendTimeout { get; set; }

        /// <summary>
        ///     Max size of one message, used for verify size and throw exception.
        ///     Default: 1 M
        /// </summary>
        public int MaxMessageSize { get; set; }

        public string ClientId { get; set; }

        /// <summary>
        ///     https://cwiki.apache.org/confluence/display/KAFKA/A+Guide+To+The+Kafka+Protocol#AGuideToTheKafkaProtocol-ProduceRequest
        ///     RequiredAcks: This field indicates how many acknowledgements the servers should receive before responding to the
        ///     request.
        ///     If it is 0 the server will not send any response (this is the only case where the server will not reply to a
        ///     request).
        ///     If it is 1, the server will wait the data is written to the local log before sending a response.
        ///     If it is -1 the server will block until the message is committed by all in sync replicas before sending a response.
        ///     For any number > 1 the server will block waiting for this number of acknowledgements to occur (but the server will
        ///     never wait for
        ///     more acknowledgements than there are in-sync replicas).
        /// </summary>
        public short RequiredAcks { get; set; }

        /// <summary>
        ///     ACK timeout, default 300ms.
        ///     If you set RequiredAcks as one big value or -1, need change this value big.
        /// </summary>
        public int AckTimeout { get; set; }
    }
}