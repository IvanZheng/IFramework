using System;
using System.Collections.Generic;
using System.Linq;
using IFramework.Infrastructure.Logging;
using IFramework.IoC;
using Kafka.Client.Cluster;
using Kafka.Client.Exceptions;
using Kafka.Client.Requests;
using Kafka.Client.Utils;
using Kafka.Client.ZooKeeperIntegration;

namespace Kafka.Client.Producers.Partitioning
{
    public class BrokerPartitionInfo : IBrokerPartitionInfo
    {
        public static ILogger Logger = IoCFactory.Resolve<ILoggerFactory>().Create(typeof(BrokerPartitionInfo));

        private readonly ISyncProducerPool syncProducerPool;

        private readonly Dictionary<string, Dictionary<int, int[]>> topicDataInZookeeper =
            new Dictionary<string, Dictionary<int, int[]>>();

        private readonly IDictionary<string, TopicMetadata> topicPartitionInfo = new Dictionary<string, TopicMetadata>()
            ;

        private readonly IDictionary<string, DateTime> topicPartitionInfoLastUpdateTime =
            new Dictionary<string, DateTime>();

        private readonly Dictionary<string, List<Partition>> topicPartitionInfoList =
            new Dictionary<string, List<Partition>>();

        private readonly object updateLock = new object();
        private readonly ZooKeeperClient zkClient;
        private readonly int topicMetaDataRefreshIntervalMS;

        public BrokerPartitionInfo(ISyncProducerPool syncProducerPool, IDictionary<string, TopicMetadata> cache,
            IDictionary<string, DateTime> lastUpdateTime, int topicMetaDataRefreshIntervalMS, ZooKeeperClient zkClient)
        {
            this.syncProducerPool = syncProducerPool;
            topicPartitionInfo = cache;
            topicPartitionInfoLastUpdateTime = lastUpdateTime;
            this.topicMetaDataRefreshIntervalMS = topicMetaDataRefreshIntervalMS;
            this.zkClient = zkClient;
        }

        public BrokerPartitionInfo(ISyncProducerPool syncProducerPool)
        {
            this.syncProducerPool = syncProducerPool;
        }

        /// <summary>
        ///     Return leader of each partition.
        /// </summary>
        /// <param name="versionId"></param>
        /// <param name="clientId"></param>
        /// <param name="correlationId"></param>
        /// <param name="topic"></param>
        /// <returns></returns>
        public List<Partition> GetBrokerPartitionInfo(short versionId, string clientId, int correlationId, string topic)
        {
            UpdateInfoInternal(versionId, correlationId, clientId, topic);
            return GetBrokerPartitionInfo(topic);
        }

        public List<Partition> GetBrokerPartitionInfo(string topic)
        {
            if (!topicPartitionInfoList.ContainsKey(topic))
                throw new KafkaException(string.Format("There is no  metadata  for topic {0} ", topic));

            var metadata = topicPartitionInfo[topic];
            if (metadata.Error != ErrorMapping.NoError)
                throw new KafkaException(
                    string.Format("The metadata status for topic {0} is abnormal, detail: ", topic), metadata.Error);

            return topicPartitionInfoList[topic];
        }

        public IDictionary<int, Broker> GetBrokerPartitionLeaders(short versionId, string clientId, int correlationId,
            string topic)
        {
            UpdateInfoInternal(versionId, correlationId, clientId, topic);
            return GetBrokerPartitionLeaders(topic);
        }

        public IDictionary<int, Broker> GetBrokerPartitionLeaders(string topic)
        {
            var metadata = topicPartitionInfo[topic];
            if (metadata.Error != ErrorMapping.NoError)
                throw new KafkaException(
                    string.Format("The metadata status for topic {0} is abnormal, detail: ", topic), metadata.Error);

            var partitionLeaders = new Dictionary<int, Broker>();
            foreach (var p in metadata.PartitionsMetadata)
            {
                if (p.Leader != null && !partitionLeaders.ContainsKey(p.PartitionId))
                {
                    partitionLeaders.Add(p.PartitionId, p.Leader);
                    Logger.DebugFormat("Topic {0} partition {1} has leader {2}", topic,
                        p.PartitionId, p.Leader.Id);
                }

                if (p.Leader == null)
                    Logger.DebugFormat("Topic {0} partition {1} does not have a leader yet", topic,
                        p.PartitionId);
            }
            return partitionLeaders;
        }

        /// <summary>
        ///     Force get topic metadata and update
        /// </summary>
        public void UpdateInfo(short versionId, int correlationId, string clientId, string topic)
        {
            Logger.InfoFormat("Will update metadata for topic:{0}", topic);
            Guard.NotNullNorEmpty(topic, "topic");
            var shuffledBrokers = syncProducerPool.GetShuffledProducers();
            var i = 0;
            var hasFetchedInfo = false;
            while (i < shuffledBrokers.Count && !hasFetchedInfo)
            {
                var producer = shuffledBrokers[i++];

                try
                {
                    var topicMetadataRequest = TopicMetadataRequest.Create(new List<string> {topic}, versionId,
                        correlationId, clientId);
                    var topicMetadataList = producer.Send(topicMetadataRequest);
                    var topicMetadata = topicMetadataList.Any() ? topicMetadataList.First() : null;
                    if (topicMetadata != null)
                        if (topicMetadata.Error != ErrorMapping.NoError)
                        {
                            Logger.WarnFormat("Try get metadata of topic {0} from {1}({2}) . Got error: {3}", topic,
                                producer.Config.BrokerId, producer.Config.Host, topicMetadata.Error.ToString());
                        }
                        else
                        {
                            topicPartitionInfo[topic] = topicMetadata;
                            topicPartitionInfoLastUpdateTime[topic] = DateTime.UtcNow;
                            Logger.InfoFormat("Will  Update  metadata info, topic {0} ", topic);

                            //TODO:  For all partitions which has metadata, here return the sorted list.
                            //But sometimes kafka didn't return metadata for all topics.
                            topicPartitionInfoList[topic] = topicMetadata.PartitionsMetadata.Select(m =>
                                {
                                    var partition = new Partition(topic, m.PartitionId);
                                    if (m.Leader != null)
                                    {
                                        var leaderReplica = new Replica(m.Leader.Id, topic);
                                        partition.Leader = leaderReplica;
                                        Logger.InfoFormat("Topic {0} partition {1} has leader {2}", topic,
                                            m.PartitionId, m.Leader.Id);

                                        return partition;
                                    }

                                    Logger.WarnFormat("Topic {0} partition {1} does not have a leader yet", topic,
                                        m.PartitionId);

                                    return partition;
                                }
                            ).OrderBy(x => x.PartId).ToList();
                            ;
                            hasFetchedInfo = true;
                            Logger.InfoFormat("Finish  Update  metadata info, topic {0}  Partitions:{1}  No leader:{2}",
                                topic, topicPartitionInfoList[topic].Count,
                                topicPartitionInfoList[topic].Where(r => r.Leader == null).Count());

                            //In very weired case, the kafka broker didn't return metadata of all broker. need break and retry.  https://issues.apache.org/jira/browse/KAFKA-1998
                            // http://qnalist.com/questions/5899394/topicmetadata-response-miss-some-partitions-information-sometimes
                            if (zkClient != null)
                            {
                                var topicMetaDataInZookeeper = ZkUtils.GetTopicMetadataInzookeeper(zkClient, topic);
                                if (topicMetaDataInZookeeper != null && topicMetaDataInZookeeper.Any())
                                {
                                    topicDataInZookeeper[topic] = topicMetaDataInZookeeper;
                                    if (topicPartitionInfoList[topic].Count != topicMetaDataInZookeeper.Count)
                                    {
                                        Logger.ErrorFormat(
                                            "NOT all partition has metadata.  Topic partition in zookeeper :{0} topics has partition metadata: {1}",
                                            topicMetaDataInZookeeper.Count, topicPartitionInfoList[topic].Count);
                                        throw new UnavailableProducerException(string.Format(
                                            "Please make sure every partition at least has one broker running and retry again.   NOT all partition has metadata.  Topic partition in zookeeper :{0} topics has partition metadata: {1}",
                                            topicMetaDataInZookeeper.Count, topicPartitionInfoList[topic].Count));
                                    }
                                }
                            }
                        }
                }
                catch (Exception e)
                {
                    Logger.ErrorFormat("Try get metadata of topic {0} from {1}({2}) . Got error: {3}", topic,
                        producer.Config.BrokerId, producer.Config.Host, e.FormatException());
                }
            }
        }

        private void UpdateInfoInternal(short versionId, int correlationId, string clientId, string topic)
        {
            Logger.DebugFormat("Will try check if need update. broker partition info for topic {0}", topic);
            //check if the cache has metadata for this topic
            var needUpdateForNotExists = false;
            var needUpdateForExpire = false;
            if (!topicPartitionInfo.ContainsKey(topic) || topicPartitionInfo[topic].Error != ErrorMapping.NoError)
                needUpdateForNotExists = true;

            if (topicPartitionInfoLastUpdateTime.ContainsKey(topic)
                && (DateTime.UtcNow - topicPartitionInfoLastUpdateTime[topic]).TotalMilliseconds >
                topicMetaDataRefreshIntervalMS)
            {
                needUpdateForExpire = true;
                Logger.InfoFormat("Will update metadata for topic:{0}  Last update time:{1}  Diff:{2} Config:{3} ",
                    topic, topicPartitionInfoLastUpdateTime[topic]
                    , (DateTime.UtcNow - topicPartitionInfoLastUpdateTime[topic]).TotalMilliseconds,
                    topicMetaDataRefreshIntervalMS);
            }

            if (needUpdateForNotExists || needUpdateForExpire)
            {
                Logger.InfoFormat(
                    "Will update metadata for topic:{0} since: needUpdateForNotExists: {1}  needUpdateForExpire:{2} ",
                    topic, needUpdateForNotExists, needUpdateForExpire);
                lock (updateLock)
                {
                    UpdateInfo(versionId, correlationId, clientId, topic);
                    if (!topicPartitionInfo.ContainsKey(topic))
                        throw new KafkaException(
                            string.Format("Failed to fetch topic metadata for topic: {0} ", topic));
                }
            }
        }
    }
}