using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using IFramework.Infrastructure.Logging;
using IFramework.IoC;
using Kafka.Client.Cfg;
using Kafka.Client.Consumers;
using Kafka.Client.Exceptions;
using Kafka.Client.Requests;
using Kafka.Client.ZooKeeperIntegration;
using ZooKeeperNet;

namespace Kafka.Client.Utils
{
    /// <summary>
    ///     Helper class to collect statistics about Consumer's offsets, lags, etc.
    /// </summary>
    public class ConsumerOffsetChecker : KafkaClientBase
    {
        public static ILogger Logger = IoCFactory.Resolve<ILoggerFactory>().Create(typeof(ConsumerOffsetChecker));
        private readonly ConsumerConfiguration config;

        private readonly IDictionary<int, Consumer> consumerDict = new Dictionary<int, Consumer>();
        private readonly object shuttingDownLock = new object();
        private volatile bool disposed;
        private Cluster.Cluster kafkaCluster;
        private IZooKeeperClient zkClient;

        public ConsumerOffsetChecker(ConsumerConfiguration config)
        {
            this.config = config;
            ConnectZk();
        }

        /// <summary>
        ///     Get the statistics about how many messages left in queue unconsumed by specified consumer group
        /// </summary>
        /// <param name="topics">
        ///     Topics name that Consumer Group started consuming. If topics are null
        ///     <see cref="Kafka.Client.Utils.ConsumerOffsetChecker" /> will automatically retrieve all topics for specified
        ///     Consumer Group
        /// </param>
        /// <param name="consumerGroup">Consumer group name</param>
        /// <returns>
        ///     <see cref="Kafka.Client.Consumers.ConsumerGroupStatisticsRecord" /> that contains statistics of consuming
        ///     specified topics by specified consumer group. If topics are null
        ///     <see cref="Kafka.Client.Utils.ConsumerOffsetChecker" /> will automatically retrieve all topics for specified
        ///     Consumer Group
        /// </returns>
        /// <remarks>
        ///     If topics are null <see cref="Kafka.Client.Utils.ConsumerOffsetChecker" /> will automatically retrieve all
        ///     topics for specified Consumer Group
        /// </remarks>
        public ConsumerGroupStatisticsRecord GetConsumerStatistics(ICollection<string> topics, string consumerGroup)
        {
            // retrive latest brokers info
            RefreshKafkaBrokersInfo();

            if (topics == null)
            {
                IEnumerable<string> zkTopics;
                try
                {
                    zkTopics = zkClient.GetChildren(new ZKGroupDirs(consumerGroup).ConsumerGroupDir + "/offsets");
                }
                catch (KeeperException e)
                {
                    if (e.ErrorCode == KeeperException.Code.NONODE)
                        zkTopics = null;
                    else
                        throw;
                }

                if (zkTopics == null)
                    throw new ArgumentException(
                        string.Format(CultureInfo.InvariantCulture,
                            "Can't automatically retrieve topic list because consumer group {0} does not have any topics with commited offsets",
                            consumerGroup));

                topics = zkTopics.ToList();
            }

            Logger.InfoFormat("Collecting consumer offset statistics for consumer group {0}", consumerGroup);

            var consumerGroupState = new ConsumerGroupStatisticsRecord
            {
                ConsumerGroupName = consumerGroup,
                TopicsStat = new Dictionary<string, TopicStatisticsRecord>(topics.Count)
            };
            try
            {
                foreach (var topic in topics)
                {
                    Logger.DebugFormat("Collecting consumer offset statistics for consumer group {0}, topic {1}",
                        consumerGroup, topic);
                    consumerGroupState.TopicsStat[topic] = ProcessTopic(consumerGroup, topic);
                }
            }
            catch (NoPartitionsForTopicException exc)
            {
                Logger.ErrorFormat("Could not find any partitions for topic {0}. Error: {1}", exc.Topic,
                    exc.FormatException());
                throw;
            }
            catch (Exception exc)
            {
                Logger.ErrorFormat("Failed to collect consumer offset statistics for consumer group {0}. Error: {1}",
                    consumerGroup, exc.FormatException());
                throw;
            }
            finally
            {
                // close created consumers
                foreach (var consumer in consumerDict)
                    consumer.Value.Dispose();

                consumerDict.Clear();
            }

            Logger.InfoFormat("Collecting consumer offset statistics for consumer group {0} completed", consumerGroup);

            return consumerGroupState;
        }

        private void RefreshKafkaBrokersInfo()
        {
            kafkaCluster = new Cluster.Cluster(zkClient);
        }

        private TopicStatisticsRecord ProcessTopic(string consumerGroup, string topic)
        {
            var topicPartitionMap = ZkUtils.GetPartitionsForTopics(zkClient, new[] {topic});

            var topicState = new TopicStatisticsRecord
            {
                Topic = topic,
                PartitionsStat = new Dictionary<int, PartitionStatisticsRecord>(topicPartitionMap[topic].Count)
            };

            foreach (var partitionId in topicPartitionMap[topic])
                try
                {
                    var partitionIdInt = int.Parse(partitionId);
                    topicState.PartitionsStat[partitionIdInt] = ProcessPartition(consumerGroup, topic, partitionIdInt);
                }
                catch (NoLeaderForPartitionException exc)
                {
                    Logger.ErrorFormat("Could not found a leader for partition {0}. Details: {1}", exc.PartitionId,
                        exc.FormatException());
                }
                catch (BrokerNotAvailableException exc)
                {
                    Logger.ErrorFormat(
                        "Could not found a broker information for broker {0} while processing partition {1}. Details: {2}",
                        exc.BrokerId, partitionId, exc.FormatException());
                }
                catch (OffsetIsUnknowException exc)
                {
                    Logger.ErrorFormat(
                        "Could not retrieve offset from broker {0} for topic {1} partition {2}. Details: {3}",
                        exc.BrokerId, exc.Topic, exc.PartitionId, exc.FormatException());
                }

            return topicState;
        }

        private PartitionStatisticsRecord ProcessPartition(string consumerGroup, string topic, int partitionId)
        {
            Logger.DebugFormat("Collecting consumer offset statistics for consumer group {0}, topic {1}, partition {2}",
                consumerGroup, topic, partitionId);

            // find partition leader and create a consumer instance
            var leader = ZkUtils.GetLeaderForPartition(zkClient, topic, partitionId);
            if (!leader.HasValue)
                throw new NoLeaderForPartitionException(partitionId);

            var consumer = GetConsumer(leader.Value);

            // get current offset
            long? currentOffset;
            var partitionIdStr = partitionId.ToString(CultureInfo.InvariantCulture);
            var znode = ZkUtils.GetConsumerPartitionOffsetPath(consumerGroup, topic, partitionIdStr);
            var currentOffsetString = zkClient.ReadData<string>(znode, true);
            if (currentOffsetString == null)
            {
                // if offset is not stored in ZooKeeper retrieve first offset from actual Broker
                currentOffset =
                    ConsumerUtils.EarliestOrLatestOffset(consumer, topic, partitionId, OffsetRequest.EarliestTime);
                if (!currentOffset.HasValue)
                    throw new OffsetIsUnknowException(topic, leader.Value, partitionId);
            }
            else
            {
                currentOffset = long.Parse(currentOffsetString);
            }

            // get last offset
            var lastOffset =
                ConsumerUtils.EarliestOrLatestOffset(consumer, topic, partitionId, OffsetRequest.LatestTime);
            if (!lastOffset.HasValue)
                throw new OffsetIsUnknowException(topic, leader.Value, partitionId);

            var owner = zkClient.ReadData<string>(
                ZkUtils.GetConsumerPartitionOwnerPath(consumerGroup, topic, partitionIdStr), true);

            return new PartitionStatisticsRecord
            {
                PartitionId = partitionId,
                CurrentOffset = currentOffset.Value,
                LastOffset = lastOffset.Value,
                OwnerConsumerId = owner
            };
        }

        /// <summary>
        ///     Get consumer instance for specified broker from cache. Or create a consumer if it does not already exist.
        /// </summary>
        /// <param name="brokerId">Broker id</param>
        /// <returns>Consumer instance for specified broker.</returns>
        private Consumer GetConsumer(int brokerId)
        {
            Consumer result;
            if (!consumerDict.TryGetValue(brokerId, out result))
            {
                var broker = kafkaCluster.GetBroker(brokerId);
                if (broker == null)
                    throw new BrokerNotAvailableException(brokerId);

                result = new Consumer(config, broker.Host, broker.Port);
                consumerDict.Add(brokerId, result);
            }

            return result;
        }

        private void ConnectZk()
        {
            Logger.InfoFormat("Connecting to zookeeper instance at {0}", config.ZooKeeper.ZkConnect);
            zkClient = new ZooKeeperClient(config.ZooKeeper.ZkConnect, config.ZooKeeper.ZkSessionTimeoutMs,
                ZooKeeperStringSerializer.Serializer, config.ZooKeeper.ZkConnectionTimeoutMs);
            zkClient.Connect();
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            if (disposed)
                return;

            Logger.Info("ConsumerOffsetChecker shutting down");

            try
            {
                zkClient.UnsubscribeAll();

                Thread.Sleep(1000);

                lock (shuttingDownLock)
                {
                    if (disposed)
                        return;

                    disposed = true;
                }

                if (zkClient != null)
                    zkClient.Dispose();
            }
            catch (Exception exc)
            {
                Logger.Debug("Ignoring unexpected errors on shutting down", exc);
            }

            Logger.Info("ConsumerOffsetChecker shut down completed");
        }
    }
}