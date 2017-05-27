using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using IFramework.Infrastructure.Logging;
using IFramework.IoC;
using Kafka.Client.Cfg;
using Kafka.Client.Cluster;
using Kafka.Client.Consumers;
using Kafka.Client.Exceptions;
using Kafka.Client.Producers;
using Kafka.Client.Producers.Partitioning;
using Kafka.Client.Producers.Sync;
using Kafka.Client.Requests;
using Kafka.Client.Utils;

namespace Kafka.Client.Helper
{
    /// <summary>
    ///     Manage :
    ///     syncProducerPool to get metadta/offset
    ///     ProducerPool to send.
    ///     ConsumerPool to consume.
    ///     new KafkaSimpleManager()
    ///     Scenario 1:
    ///     RefreshMetadata()
    ///     RefreshAndGetOffsetByTimeStamp()
    ///     Scenario 2 Produce :
    ///     Sub scenario 1:  Send to random partition
    ///     Sub scenario 2:  Send to specific partition
    ///     For Sub scenario 1 and 2:
    ///     InitializeProducerPoolForTopic
    ///     call GetProducer() to get required producer
    ///     Send
    ///     If send failed, call  RefreshMetadataAndRecreateProducerWithPartition
    ///     Sub scenario 3:  Send to partition by default partitioner class.   Math.Abs(key.GetHashCode()) % numPartitions
    ///     Sub scenario 4:  Send to partition by customized partitioner
    ///     For Sub scenario 3 and 4:
    ///     InitializeProducerPoolForTopic
    ///     call GetProducerWithPartionerClass() to get producer
    ///     Send
    ///     If send failed, call RefreshMetadata and retry.
    ///     Scenario 3 Consume pool:
    ///     call GetConsumerFromPool() to get consumer
    ///     consume
    ///     If consume failed, call  GetConsumerFromPoolAfterRecreate() to get new consumer.
    /// </summary>
    public class KafkaSimpleManager<TKey, TData> : IDisposable
    {
        public static ILogger Logger = IoCFactory.Resolve<ILoggerFactory>().Create("KafkaSimpleManager");

        private readonly ConcurrentDictionary<string, object> TopicLockProduce =
            new ConcurrentDictionary<string, object>();

        //topic --> partitionid -->
        private readonly ConcurrentDictionary<string, Dictionary<int, Tuple<Broker, BrokerConfiguration>>>
            TopicMetadataPartitionsLeaders =
                new ConcurrentDictionary<string, Dictionary<int, Tuple<Broker, BrokerConfiguration>>>();

        //topic -->  TopicMetadata
        private readonly ConcurrentDictionary<string, TopicMetadata> TopicMetadatas =
            new ConcurrentDictionary<string, TopicMetadata>();

        //topic -->  TopicMetadatasLastUpdateTime
        private readonly ConcurrentDictionary<string, DateTime> TopicMetadatasLastUpdateTime =
            new ConcurrentDictionary<string, DateTime>();

        private readonly ConcurrentDictionary<string, ConcurrentDictionary<int, long>> TopicOffsetEarliest =
            new ConcurrentDictionary<string, ConcurrentDictionary<int, long>>();

        private readonly ConcurrentDictionary<string, ConcurrentDictionary<int, long>> TopicOffsetLatest =
            new ConcurrentDictionary<string, ConcurrentDictionary<int, long>>();

        private readonly ConcurrentDictionary<string, object> TopicPartitionLockConsume =
            new ConcurrentDictionary<string, object>();

        //topic --> partitionid -->
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<int, Consumer>>
            TopicPartitionsLeaderConsumers = new ConcurrentDictionary<string, ConcurrentDictionary<int, Consumer>>();

        //topic --> partitionid -->
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<int, Producer<TKey, TData>>>
            TopicPartitionsLeaderProducers =
                new ConcurrentDictionary<string, ConcurrentDictionary<int, Producer<TKey, TData>>>();

        private object topicProducerLock = new object();

        private readonly ConcurrentDictionary<string, Producer<TKey, TData>> TopicProducersWithPartitionerClass =
            new ConcurrentDictionary<string, Producer<TKey, TData>>();

        public KafkaSimpleManager(KafkaSimpleManagerConfiguration config)
        {
            Config = config;
            RecreateSyncProducerPoolForMetadata();
        }

        public KafkaSimpleManagerConfiguration Config { get; }

        #region SyncProducerPool for metadata.

        private volatile bool disposed;

        // the pool of syncProducer for TopicMetaData requests, which retrieve PartitionMetaData, including leaders and ISRs.
        private volatile SyncProducerPool syncProducerPoolForMetaData;

        private readonly object syncProducerPoolForMetadataLock = new object();
        private Random random = new Random();
        private readonly Random randomForGetCachedProducer = new Random();

        #endregion

        #region SyncProducer Pool  ForMetadata refresh

        /// <summary>
        ///     Initialize SyncProducerPool used for get metadata by query Zookeeper
        ///     Here only build connection for all ACTIVE broker.  So client actually need regularly dispose/recreate or refresh,
        ///     for example, every 20 minutes..
        /// </summary>
        public void RecreateSyncProducerPoolForMetadata()
        {
            SyncProducerPool tempSyncProducerPool = null;
            if (Config.ZookeeperConfig != null)
            {
                // Honor Zookeeper connection string only when KafkaBroker list not provided
                var producerConfig = new ProducerConfiguration(new List<BrokerConfiguration>());
                producerConfig.ZooKeeper = Config.ZookeeperConfig;
                //This pool only for get metadata, so set SyncProducerOfOneBroker to 1 is enough.
                producerConfig.SyncProducerOfOneBroker = 1;
                tempSyncProducerPool = new SyncProducerPool(producerConfig);
            }

            if (syncProducerPoolForMetaData != null)
                lock (syncProducerPoolForMetadataLock)
                {
                    syncProducerPoolForMetaData.Dispose();
                    syncProducerPoolForMetaData = null;
                    syncProducerPoolForMetaData = tempSyncProducerPool;
                }
            else
                syncProducerPoolForMetaData = tempSyncProducerPool;

            if (syncProducerPoolForMetaData == null || syncProducerPoolForMetaData.Count() == 0)
            {
                var s = string.Format(
                    "KafkaSimpleManager[{0}] SyncProducerPool Initialization produced empty syncProducer list, please check path /brokers/ids in zookeeper={1} to make sure ther is active brokers.",
                    GetHashCode().ToString("X"), Config.ZookeeperConfig);
                Logger.Error(s);
                syncProducerPoolForMetaData = null;
                throw new ArgumentException(s);
            }
            Logger.InfoFormat("The syncProducerPoolForMetaData:{0}", syncProducerPoolForMetaData.Count());
            foreach (var kv in syncProducerPoolForMetaData.syncProducers)
                Logger.InfoFormat("\tBrokerID:  {0}  count: {1}", kv.Key, kv.Value.Producers.Count);

            Logger.InfoFormat("KafkaSimpleManager[{0}] SyncProducerPool initialized", GetHashCode().ToString("X"));
        }

        public List<string> GetTopicPartitionsFromZK(string topic)
        {
            return syncProducerPoolForMetaData.zkClient
                .GetChildren(string.Format("/brokers/topics/{0}/partitions", topic)).ToList();
        }

        /// <summary>
        ///     Reset syncProducer pool
        /// </summary>
        private void ClearSyncProducerPoolForMetadata()
        {
            if (syncProducerPoolForMetaData != null)
            {
                syncProducerPoolForMetaData.Dispose();
                syncProducerPoolForMetaData = null;
            }
            Logger.DebugFormat("KafkaSyncProducerPoolManager[{0}] SyncProducerPool cleared",
                GetHashCode().ToString("X"));
        }

        #endregion

        #region Metadata, leader, configuration

        /// <summary>
        ///     Refresh metadata of one topic and return.
        ///     If can't get metadata for specified topic at first time, will RecreateSyncProducerPoolForMetadata and retry once.
        ///     MANIFOLD use.
        /// </summary>
        public TopicMetadata RefreshMetadata(short versionId, string clientId, int correlationId, string topic,
            bool force)
        {
            Logger.InfoFormat("RefreshMetadata enter: {0} {1} {2} Topic:{3} Force:{4}", versionId, clientId,
                correlationId, topic, force);
            if (!force && TopicMetadatas.ContainsKey(topic))
                return TopicMetadatas[topic];

            var retry = 0;
            while (retry < 2)
            {
                var tempTopicMetadatas = new Dictionary<string, TopicMetadata>();
                var tempTopicMetadatasLastUpdateTime = new Dictionary<string, DateTime>();
                var partitionLeaders = new Dictionary<int, Tuple<Broker, BrokerConfiguration>>();
                RefreshMetadataInternal(versionId, clientId, correlationId, topic, tempTopicMetadatas,
                    tempTopicMetadatasLastUpdateTime, partitionLeaders);

                if (tempTopicMetadatas.ContainsKey(topic))
                {
                    TopicMetadatas[topic] = tempTopicMetadatas[topic];
                    TopicMetadatasLastUpdateTime[topic] = tempTopicMetadatasLastUpdateTime[topic];
                    TopicMetadataPartitionsLeaders[topic] = partitionLeaders;
                    var partitionCountInZK = GetTopicPartitionsFromZK(topic).Count;
                    if (partitionCountInZK != partitionLeaders.Count)
                        Logger.WarnFormat(
                            "RefreshMetadata exit return. Some partitions has no leader.  Topic:{0}  PartitionMetadata:{1} partitionLeaders:{2} != partitionCountInZK:{3}",
                            topic, tempTopicMetadatas[topic].PartitionsMetadata.Count(), partitionLeaders.Count,
                            partitionCountInZK);
                    else
                        Logger.InfoFormat(
                            "RefreshMetadata exit return. Topic:{0}  PartitionMetadata:{1} partitionLeaders:{2} partitionCountInZK:{3}",
                            topic, tempTopicMetadatas[topic].PartitionsMetadata.Count(), partitionLeaders.Count,
                            partitionCountInZK);
                    return TopicMetadatas[topic];
                }
                Logger.WarnFormat(
                    "Got null for metadata of topic {0}, will RecreateSyncProducerPoolForMetadata and retry . ", topic);
                RecreateSyncProducerPoolForMetadata();
                retry++;
            }

            Logger.WarnFormat("RefreshMetadata exit return NULL: {0} {1} {2} Topic:{3} Force:{4}", versionId, clientId,
                correlationId, topic, force);
            return null;
        }

        public TopicMetadata GetTopicMetadta(string topic)
        {
            return TopicMetadatas[topic];
        }

        /// <summary>
        ///     Get leader broker of one topic partition. without retry.
        ///     If got exception from this, probably need client code call RefreshMetadata with force = true.
        /// </summary>
        internal BrokerConfiguration GetLeaderBrokerOfPartition(string topic, int partitionID)
        {
            if (!TopicMetadatas.ContainsKey(topic))
                throw new KafkaException(string.Format(
                    "There is no  metadata  for topic {0}.  Please call RefreshMetadata with force = true and try again.",
                    topic));

            var metadata = TopicMetadatas[topic];
            if (metadata.Error != ErrorMapping.NoError)
                throw new KafkaException(
                    string.Format("The metadata status for topic {0} is abnormal, detail: ", topic), metadata.Error);

            if (!TopicMetadataPartitionsLeaders[topic].ContainsKey(partitionID))
                throw new NoLeaderForPartitionException(
                    string.Format("No leader for topic {0} parition {1} ", topic, partitionID));

            return TopicMetadataPartitionsLeaders[topic][partitionID].Item2;
        }

        private void RefreshMetadataInternal(short versionId, string clientId, int correlationId, string topic,
            Dictionary<string, TopicMetadata> tempTopicMetadatas,
            Dictionary<string, DateTime> tempTopicMetadatasLastUpdateTime,
            Dictionary<int, Tuple<Broker, BrokerConfiguration>> partitionLeaders)
        {
            Logger.InfoFormat("RefreshMetadataInternal enter: {0} {1} {2} Topic:{3} ", versionId, clientId,
                correlationId, topic);

            lock (syncProducerPoolForMetadataLock)
            {
                var brokerPartitionInfo = new BrokerPartitionInfo(syncProducerPoolForMetaData, tempTopicMetadatas,
                    tempTopicMetadatasLastUpdateTime, ProducerConfiguration.DefaultTopicMetaDataRefreshIntervalMS,
                    syncProducerPoolForMetaData.zkClient);
                brokerPartitionInfo.UpdateInfo(versionId, correlationId, clientId, topic);
            }
            if (!tempTopicMetadatas.ContainsKey(topic))
                throw new NoBrokerForTopicException(
                    string.Format(
                        "There is no metadata for topic {0}.  Please check if all brokers of that topic live.", topic));
            var metadata = tempTopicMetadatas[topic];
            if (metadata.Error != ErrorMapping.NoError)
            {
                throw new KafkaException(
                    string.Format("The metadata status for topic {0} is abnormal, detail: ", topic), metadata.Error);
                ;
            }

            foreach (var p in metadata.PartitionsMetadata)
            {
                if (p.Leader != null && !partitionLeaders.ContainsKey(p.PartitionId))
                {
                    partitionLeaders.Add(p.PartitionId, new Tuple<Broker, BrokerConfiguration>(
                        p.Leader,
                        new BrokerConfiguration
                        {
                            BrokerId = p.Leader.Id,
                            Host = p.Leader.Host,
                            Port = p.Leader.Port
                        }));
                    Logger.DebugFormat("RefreshMetadataInternal Topic {0} partition {1} has leader {2}", topic,
                        p.PartitionId, p.Leader.Id);
                }
                if (p.Leader == null)
                    Logger.ErrorFormat("RefreshMetadataInternal Topic {0} partition {1} does not have a leader yet.",
                        topic, p.PartitionId);
            }
            Logger.InfoFormat("RefreshMetadataInternal exit: {0} {1} {2} Topic:{3} ", versionId, clientId,
                correlationId, topic);
        }

        #endregion

        #region Offset

        /// <summary>
        ///     Get offset
        /// </summary>
        public void RefreshAndGetOffset(short versionId, string clientId, int correlationId, string topic,
            int partitionId, bool forceRefreshOffsetCache, out long earliestOffset, out long latestOffset)
        {
            earliestOffset = -1;
            latestOffset = -1;
            if (!forceRefreshOffsetCache && TopicOffsetEarliest.ContainsKey(topic) &&
                TopicOffsetEarliest[topic].ContainsKey(partitionId))
                earliestOffset = TopicOffsetEarliest[topic][partitionId];
            if (!forceRefreshOffsetCache && TopicOffsetLatest.ContainsKey(topic) &&
                TopicOffsetLatest[topic].ContainsKey(partitionId))
                latestOffset = TopicOffsetLatest[topic][partitionId];
            if (!forceRefreshOffsetCache && earliestOffset != -1 && latestOffset != -1)
                return;
            //Get
            using (var consumer = GetConsumer(topic, partitionId))
            {
                var offsetRequestInfoEarliest = new Dictionary<string, List<PartitionOffsetRequestInfo>>();
                var offsetRequestInfoForPartitionsEarliest = new List<PartitionOffsetRequestInfo>();
                offsetRequestInfoForPartitionsEarliest.Add(
                    new PartitionOffsetRequestInfo(partitionId, OffsetRequest.EarliestTime, 1));
                offsetRequestInfoEarliest.Add(topic, offsetRequestInfoForPartitionsEarliest);
                var offsetRequestEarliest = new OffsetRequest(offsetRequestInfoEarliest);
                //Earliest
                var offsetResponseEarliest = consumer.GetOffsetsBefore(offsetRequestEarliest);
                List<PartitionOffsetsResponse> partitionOffsetEaliest = null;
                if (offsetResponseEarliest.ResponseMap.TryGetValue(topic, out partitionOffsetEaliest))
                    foreach (var p in partitionOffsetEaliest)
                        if (p.Error == ErrorMapping.NoError && p.PartitionId == partitionId)
                        {
                            earliestOffset = p.Offsets[0];
                            //Cache                           
                            if (!TopicOffsetEarliest.ContainsKey(topic))
                                TopicOffsetEarliest.TryAdd(topic, new ConcurrentDictionary<int, long>());
                            TopicOffsetEarliest[topic][partitionId] = earliestOffset;
                        }

                //Latest
                var offsetRequestInfoLatest = new Dictionary<string, List<PartitionOffsetRequestInfo>>();
                var offsetRequestInfoForPartitionsLatest = new List<PartitionOffsetRequestInfo>();
                offsetRequestInfoForPartitionsLatest.Add(
                    new PartitionOffsetRequestInfo(partitionId, OffsetRequest.LatestTime, 1));
                offsetRequestInfoLatest.Add(topic, offsetRequestInfoForPartitionsLatest);
                var offsetRequestLatest = new OffsetRequest(offsetRequestInfoLatest);

                var offsetResponseLatest = consumer.GetOffsetsBefore(offsetRequestLatest);
                List<PartitionOffsetsResponse> partitionOffsetLatest = null;
                if (offsetResponseLatest.ResponseMap.TryGetValue(topic, out partitionOffsetLatest))
                    foreach (var p in partitionOffsetLatest)
                        if (p.Error == ErrorMapping.NoError && p.PartitionId == partitionId)
                        {
                            latestOffset = p.Offsets[0];
                            //Cache
                            if (!TopicOffsetLatest.ContainsKey(topic))
                                TopicOffsetLatest.TryAdd(topic, new ConcurrentDictionary<int, long>());
                            TopicOffsetLatest[topic][partitionId] = latestOffset;
                        }
            }
        }

        public long RefreshAndGetOffsetByTimeStamp(short versionId, string clientId, int correlationId, string topic,
            int partitionId, DateTime timeStampInUTC)
        {
            //Get
            using (var consumer = GetConsumer(topic, partitionId))
            {
                var offsetRequestInfoEarliest = new Dictionary<string, List<PartitionOffsetRequestInfo>>();
                var offsetRequestInfoForPartitionsEarliest = new List<PartitionOffsetRequestInfo>();
                offsetRequestInfoForPartitionsEarliest.Add(new PartitionOffsetRequestInfo(partitionId,
                    KafkaClientHelperUtils.ToUnixTimestampMillis(timeStampInUTC), 8));
                offsetRequestInfoEarliest.Add(topic, offsetRequestInfoForPartitionsEarliest);
                var offsetRequestEarliest = new OffsetRequest(offsetRequestInfoEarliest);
                //Earliest
                var offsetResponseEarliest = consumer.GetOffsetsBefore(offsetRequestEarliest);
                List<PartitionOffsetsResponse> partitionOffsetByTimeStamp = null;
                if (offsetResponseEarliest.ResponseMap.TryGetValue(topic, out partitionOffsetByTimeStamp))
                    foreach (var p in partitionOffsetByTimeStamp)
                        if (p.PartitionId == partitionId)
                            return partitionOffsetByTimeStamp[0].Offsets[0];
            }
            return -1;
        }

        #endregion


        #region Thread safe producer pool

        public int InitializeProducerPoolForTopic(short versionId, string clientId, int correlationId, string topic,
            bool forceRefreshMetadata, ProducerConfiguration producerConfigTemplate, bool forceRecreateEvenHostPortSame)
        {
            Logger.InfoFormat(
                "InitializeProducerPoolForTopic ==  enter:  Topic:{0} forceRefreshMetadata:{1}  forceRecreateEvenHostPortSame:{2} ",
                topic, forceRefreshMetadata, forceRecreateEvenHostPortSame);
            TopicMetadata topicMetadata = null;
            if (forceRefreshMetadata)
                topicMetadata = RefreshMetadata(versionId, clientId, correlationId, topic, forceRefreshMetadata);

            var partitionLeaders = TopicMetadataPartitionsLeaders[topic];

            //TODO: but some times the partition maybe has no leader 

            //For use partitioner calss.  Totally only create one producer. 
            if (string.IsNullOrEmpty(Config.PartitionerClass))
            {
                foreach (var kv in partitionLeaders)
                    CreateProducerOfOnePartition(topic, kv.Key, kv.Value.Item2, producerConfigTemplate,
                        forceRecreateEvenHostPortSame);
                Logger.InfoFormat(
                    "InitializeProducerPoolForTopic ==  exit:  Topic:{0} forceRefreshMetadata:{1}  forceRecreateEvenHostPortSame:{2} this.TopicPartitionsLeaderProducers[topic].Count:{3}",
                    topic, forceRefreshMetadata, forceRecreateEvenHostPortSame,
                    TopicPartitionsLeaderProducers[topic].Count);
                return TopicPartitionsLeaderProducers[topic].Count;
            }
            var producerConfig = new ProducerConfiguration(producerConfigTemplate);
            producerConfig.ZooKeeper = Config.ZookeeperConfig;
            producerConfig.PartitionerClass = Config.PartitionerClass;

            Producer<TKey, TData> oldProducer = null;
            if (TopicProducersWithPartitionerClass.TryGetValue(topic, out oldProducer))
            {
                var removeOldProducer = false;
                if (oldProducer != null)
                {
                    oldProducer.Dispose();
                    removeOldProducer = TopicProducersWithPartitionerClass.TryRemove(topic, out oldProducer);
                    Logger.InfoFormat(
                        "InitializeProducerPoolForTopic == Remove producer from TopicProducersWithPartitionerClass for  topic {0}  removeOldProducer:{1} ",
                        topic, removeOldProducer);
                }
            }

            var producer = new Producer<TKey, TData>(producerConfig);
            var addNewProducer = TopicProducersWithPartitionerClass.TryAdd(topic, producer);
            Logger.InfoFormat(
                "InitializeProducerPoolForTopic == Add producer  TopicProducersWithPartitionerClass for  topic {0}  SyncProducerOfOneBroker:{1} addNewProducer:{2}   END.",
                topic, producerConfig.SyncProducerOfOneBroker, addNewProducer);

            return addNewProducer ? 1 : 0;
        }

        /// <summary>
        ///     Get Producer once Config.PartitionerClass is one valid class full name.
        /// </summary>
        /// <param name="topic"></param>
        /// <returns></returns>
        public Producer<TKey, TData> GetProducerWithPartionerClass(string topic)
        {
            if (string.IsNullOrEmpty(Config.PartitionerClass))
                throw new ArgumentException(
                    "Please must specify KafkaSimpleManagerConfiguration.PartitionerClass before call this function.");
            if (!TopicProducersWithPartitionerClass.ContainsKey(topic))
                throw new KafkaException(string.Format(
                    "There is no  TopicProducersWithPartitionerClass producer  for topic {0}  , please make sure you have called CreateProducerForPartitions",
                    topic));

            return TopicProducersWithPartitionerClass[topic];
        }

        /// <summary>
        ///     Get Producer once  Config.PartitionerClass is null or empty.
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="partitionID"></param>
        /// <param name="randomReturnIfProducerOfTargetPartionNotExists"></param>
        /// <returns></returns>
        public Producer<TKey, TData> GetProducerOfPartition(string topic, int partitionID,
            bool randomReturnIfProducerOfTargetPartionNotExists)
        {
            if (!string.IsNullOrEmpty(Config.PartitionerClass))
                throw new ArgumentException(string.Format(
                    "Please call GetProducerWithPartionerClass to get producer since  KafkaSimpleManagerConfiguration.PartitionerClass is {0} is not null.",
                    Config.PartitionerClass));

            if (!TopicMetadatas.ContainsKey(topic))
                throw new KafkaException(string.Format(
                    "There is no  metadata  for topic {0}.  Please call RefreshMetadata with force = true and try again. ",
                    topic));

            if (TopicMetadatas[topic].Error != ErrorMapping.NoError)
                throw new KafkaException(
                    string.Format("The metadata status for topic {0} is abnormal, detail: ", topic),
                    TopicMetadatas[topic].Error);

            if (!TopicPartitionsLeaderProducers.ContainsKey(topic))
                throw new KafkaException(string.Format(
                    "There is no  producer  for topic {0} partition {1} , please make sure you have called CreateProducerForPartitions",
                    topic, partitionID));

            if (TopicPartitionsLeaderProducers[topic].ContainsKey(partitionID))
                return TopicPartitionsLeaderProducers[topic][partitionID];

            if (randomReturnIfProducerOfTargetPartionNotExists)
            {
                if (!TopicPartitionsLeaderProducers[topic].Any())
                    return null;

                var randomPartition = randomForGetCachedProducer.Next(TopicPartitionsLeaderProducers[topic].Count);
                return TopicPartitionsLeaderProducers[topic].Values.ElementAt(randomPartition);
            }
            return null;
        }

        public Producer<TKey, TData> RefreshMetadataAndRecreateProducerOfOnePartition(short versionId, string clientId,
            int correlationId, string topic, int partitionId, bool forceRefreshMetadata,
            bool forceRecreateEvenHostPortSame, ProducerConfiguration producerConfigTemplate,
            bool randomReturnIfProducerOfTargetPartionNotExists)
        {
            Logger.InfoFormat(
                "RefreshMetadataAndRecreateProducerWithPartition ==  enter:  Topic:{0}  partitionId:{1} forceRefreshMetadata:{2}  forceRecreateEvenHostPortSame:{3} randomReturnIfProducerOfTargetPartionNotExists:{4} ",
                topic, partitionId, forceRefreshMetadata, forceRecreateEvenHostPortSame,
                randomReturnIfProducerOfTargetPartionNotExists);

            TopicMetadata topicMetadata = null;
            if (forceRefreshMetadata)
                topicMetadata = RefreshMetadata(versionId, clientId, correlationId, topic, forceRefreshMetadata);

            if (!TopicMetadataPartitionsLeaders[topic].ContainsKey(partitionId))
                throw new NoLeaderForPartitionException(
                    string.Format("No leader for topic {0} parition {1} ", topic, partitionId));

            var value = TopicMetadataPartitionsLeaders[topic][partitionId];

            CreateProducerOfOnePartition(topic, partitionId, value.Item2, producerConfigTemplate,
                forceRecreateEvenHostPortSame);

            return GetProducerOfPartition(topic, partitionId, randomReturnIfProducerOfTargetPartionNotExists);
        }

        private void CreateProducerOfOnePartition(string topic, int partitionId, BrokerConfiguration broker,
            ProducerConfiguration producerConfigTemplate, bool forceRecreateEvenHostPortSame)
        {
            Logger.InfoFormat(
                "CreateProducer ==  enter:  Topic:{0} partitionId:{1} broker:{2}  forceRecreateEvenHostPortSame:{3} ",
                topic, partitionId, broker, forceRecreateEvenHostPortSame);
            //Explicitly set partitionID
            var producerConfig = new ProducerConfiguration(producerConfigTemplate,
                new List<BrokerConfiguration> {broker}, partitionId)
            {
                ForceToPartition = partitionId
            };

            if (!TopicPartitionsLeaderProducers.ContainsKey(topic))
                TopicPartitionsLeaderProducers.TryAdd(topic, new ConcurrentDictionary<int, Producer<TKey, TData>>());

            var dictPartitionLeaderProducersOfOneTopic = TopicPartitionsLeaderProducers[topic];
            var needRecreate = true;
            Producer<TKey, TData> oldProducer = null;
            if (dictPartitionLeaderProducersOfOneTopic.TryGetValue(partitionId, out oldProducer))
                if (oldProducer.Config.Brokers.Any()
                    && oldProducer.Config.Brokers[0].Equals(producerConfig.Brokers[0]))
                    needRecreate = false;

            if (forceRecreateEvenHostPortSame)
                needRecreate = true;

            if (needRecreate)
                lock (GetProduceLockOfTopic(topic, partitionId))
                {
                    if (dictPartitionLeaderProducersOfOneTopic.TryGetValue(partitionId, out oldProducer))
                        if (oldProducer.Config.Brokers.Any()
                            && oldProducer.Config.Brokers[0].Equals(producerConfig.Brokers[0]))
                            needRecreate = false;

                    if (forceRecreateEvenHostPortSame)
                        needRecreate = true;

                    if (!needRecreate)
                    {
                        Logger.InfoFormat(
                            "CreateProducer == Add producer SKIP  after got lock for  topic {0}  partition {1} since leader {2} not changed. Maybe created by other thread.  END.",
                            topic, partitionId, producerConfig.Brokers[0]);
                        return;
                    }

                    var removeOldProducer = false;
                    if (oldProducer != null)
                    {
                        oldProducer.Dispose();
                        removeOldProducer =
                            dictPartitionLeaderProducersOfOneTopic.TryRemove(partitionId, out oldProducer);
                        Logger.InfoFormat(
                            "CreateProducer == Remove producer for  topic {0}  partition {1} leader {2} removeOldProducer:{3} ",
                            topic, partitionId, oldProducer.Config.Brokers[0], removeOldProducer);
                    }

                    var producer = new Producer<TKey, TData>(producerConfig);
                    var addNewProducer = dictPartitionLeaderProducersOfOneTopic.TryAdd(partitionId, producer);

                    Logger.InfoFormat(
                        "CreateProducer == Add producer for  topic {0}  partition {1} leader {2} SyncProducerOfOneBroker:{3} removeOldProducer:{4} addNewProducer:{5}   END.",
                        topic, partitionId, broker, producerConfig.SyncProducerOfOneBroker, removeOldProducer,
                        addNewProducer);
                }
            else
                Logger.InfoFormat(
                    "CreateProducer == Add producer SKIP for  topic {0}  partition {1} since leader {2} not changed.   END.",
                    topic, partitionId, producerConfig.Brokers[0]);
        }

        #endregion


        #region Get consumer object

        /// <summary>
        ///     Get Consumer object from current cached metadata information without retry.
        ///     So maybe got exception if the related metadata not exists.
        ///     When create ConsumerConfiguration, will take BufferSize and FetchSize from KafkaSimpleManagerConfiguration
        ///     Client side need handle exception and the metadata change
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="partitionID"></param>
        /// <returns></returns>
        public Consumer GetConsumer(string topic, int partitionID)
        {
            return new Consumer(new ConsumerConfiguration
            {
                Broker = GetLeaderBrokerOfPartition(topic, partitionID),
                BufferSize = Config.BufferSize,
                FetchSize = Config.FetchSize,
                Verbose = Config.Verbose
            });
        }

        /// <summary>
        ///     Get Consumer object from current cached metadata information without retry.
        ///     So maybe got exception if the related metadata not exists.
        ///     When create ConsumerConfiguration, will take values in cosumerConfigTemplate.
        ///     Client side need handle exception and the metadata change
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="partitionID"></param>
        /// <param name="cosumerConfigTemplate"></param>
        /// <returns></returns>
        public Consumer GetConsumer(string topic, int partitionID, ConsumerConfiguration cosumerConfigTemplate)
        {
            var config = new ConsumerConfiguration(cosumerConfigTemplate,
                GetLeaderBrokerOfPartition(topic, partitionID));
            return new Consumer(config);
        }

        #endregion

        #region  Consumer pool

        /// <summary>
        ///     MANIFOLD use .  get one consumer from the pool.
        /// </summary>
        public Consumer GetConsumerFromPool(short versionId, string clientId, int correlationId
            , string topic, ConsumerConfiguration cosumerConfigTemplate, int partitionId)
        {
            if (!TopicPartitionsLeaderConsumers.ContainsKey(topic))
            {
                var topicMetadata = RefreshMetadata(versionId, clientId, correlationId, topic, false);
            }

            var consumers = GetConsumerPoolForTopic(topic);
            if (!consumers.ContainsKey(partitionId))
                lock (GetConsumeLockOfTopicPartition(topic, partitionId))
                {
                    if (!consumers.ContainsKey(partitionId))
                    {
                        var config = new ConsumerConfiguration(cosumerConfigTemplate,
                            GetLeaderBrokerOfPartition(topic, partitionId));
                        var consumer = new Consumer(config);
                        if (consumers.TryAdd(partitionId, consumer))
                            Logger.InfoFormat(
                                "Create one consumer for client {0} topic {1} partitoin {2} addOneConsumer return value:{3} ",
                                clientId, topic, partitionId, true);
                        else
                            Logger.WarnFormat(
                                "Create one consumer for client {0} topic {1} partitoin {2} addOneConsumer return value:{3} ",
                                clientId, topic, partitionId, false);
                    }
                }

            return consumers[partitionId];
        }

        /// <summary>
        ///     MANIFOLD use .  Force recreate consumer for some partition and return it back.
        /// </summary>
        public Consumer GetConsumerFromPoolAfterRecreate(short versionId, string clientId, int correlationId
            , string topic, ConsumerConfiguration cosumerConfigTemplate, int partitionId,
            int notRecreateTimeRangeInMs = -1)
        {
            var topicMetadata = RefreshMetadata(versionId, clientId, correlationId, topic, true);
            var consumers = GetConsumerPoolForTopic(topic);
            lock (GetConsumeLockOfTopicPartition(topic, partitionId))
            {
                var config = new ConsumerConfiguration(cosumerConfigTemplate,
                    GetLeaderBrokerOfPartition(topic, partitionId));
                Consumer oldConsumer = null;
                if (consumers.TryGetValue(partitionId, out oldConsumer))
                {
                    if ((DateTime.UtcNow.Ticks - oldConsumer.CreatedTimeInUTC) / 10000.0 < notRecreateTimeRangeInMs)
                    {
                        Logger.WarnFormat(
                            "Do NOT recreate consumer for client {0} topic {1} partitoin {2} since it only created {3} ms. less than {4} ",
                            clientId, topic, partitionId
                            , (DateTime.UtcNow.Ticks - oldConsumer.CreatedTimeInUTC) / 10000.0,
                            notRecreateTimeRangeInMs);
                    }
                    else
                    {
                        Logger.InfoFormat("Destroy one old consumer for client {0} topic {1} partitoin {2} ", clientId,
                            topic, partitionId);
                        if (oldConsumer != null)
                            oldConsumer.Dispose();

                        consumers[partitionId] = new Consumer(config);
                        Logger.InfoFormat("Create one consumer for client {0} topic {1} partitoin {2} ", clientId,
                            topic, partitionId);
                    }
                }
                else
                {
                    consumers[partitionId] = new Consumer(config);
                    Logger.InfoFormat("Newly Create one consumer for client {0} topic {1} partitoin {2} ", clientId,
                        topic, partitionId);
                }
            }

            return consumers[partitionId];
        }

        private ConcurrentDictionary<int, Consumer> GetConsumerPoolForTopic(string topic)
        {
            if (TopicPartitionsLeaderConsumers.ContainsKey(topic))
                return TopicPartitionsLeaderConsumers[topic];

            TopicPartitionsLeaderConsumers.TryAdd(topic, new ConcurrentDictionary<int, Consumer>());

            return TopicPartitionsLeaderConsumers[topic];
        }

        #endregion

        #region Dispose

        /// <summary>
        ///     Implement IDisposable
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        // Dispose(bool disposing) executes in two distinct scen/arios.
        // If disposing equals true, the method has been called directly
        // or indirectly by a user's code. Managed and unmanaged resources
        // can be disposed.
        // If disposing equals false, the method has been called by the
        // runtime from inside the finalizer and you should not reference
        // other objects. Only unmanaged resources can be disposed.
        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                    ClearSyncProducerPoolForMetadata();

                // Call the appropriate methods to clean up
                // unmanaged resources here.
                // If disposing is false,
                // only the following code is executed.
                // Note disposing has been done.
                disposed = true;
            }
        }

        #endregion

        #region lock by topic

        private object GetProduceLockOfTopic(string topic, int partitionId)
        {
            object lockOfTopicPartition = null;
            var key = topic + "_" + partitionId;
            while (!TopicLockProduce.TryGetValue(key, out lockOfTopicPartition))
            {
                lockOfTopicPartition = new object();
                if (TopicLockProduce.TryAdd(key, lockOfTopicPartition))
                    break;
            }
            return lockOfTopicPartition;
        }

        private object GetConsumeLockOfTopicPartition(string topic, int partitionId)
        {
            object lockOfTopicPartition = null;
            var key = topic + "_" + partitionId;

            while (!TopicPartitionLockConsume.TryGetValue(key, out lockOfTopicPartition))
            {
                lockOfTopicPartition = new object();
                if (TopicPartitionLockConsume.TryAdd(key, lockOfTopicPartition))
                    break;
            }
            return lockOfTopicPartition;
        }

        #endregion
    }
}