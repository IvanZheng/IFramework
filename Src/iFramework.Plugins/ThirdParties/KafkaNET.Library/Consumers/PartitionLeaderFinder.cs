using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using IFramework.Infrastructure.Logging;
using IFramework.IoC;
using Kafka.Client.Cfg;
using Kafka.Client.Cluster;
using Kafka.Client.Requests;

namespace Kafka.Client.Consumers
{
    internal class PartitionLeaderFinder
    {
        private const string clientId = "LeaderFetcher";
        public static ILogger Logger = IoCFactory.Resolve<ILoggerFactory>().Create(typeof(PartitionLeaderFinder));

        private static readonly int FailureRetryDelayMs = (int) TimeSpan.FromSeconds(5).TotalMilliseconds;

        private readonly Cluster.Cluster _brokers;

        private readonly ConsumerConfiguration _config;

        private readonly Action<PartitionTopicInfo, Broker> _createNewFetcher;

        private readonly ConcurrentQueue<PartitionTopicInfo> _partitionsNeedingLeader;

        private volatile bool _stop;

        public PartitionLeaderFinder(ConcurrentQueue<PartitionTopicInfo> partitionsNeedingLeaders,
                                     Cluster.Cluster brokers,
                                     ConsumerConfiguration config,
                                     Action<PartitionTopicInfo, Broker> createNewFetcher)
        {
            _partitionsNeedingLeader = partitionsNeedingLeaders;
            _brokers = brokers;
            _config = config;
            _createNewFetcher = createNewFetcher;
        }

        public void Start()
        {
            while (!_stop)
            {
                try
                {
                    if (_partitionsNeedingLeader.IsEmpty)
                    {
                        Thread.Sleep(_config.ConsumeGroupFindNewLeaderSleepIntervalMs);
                        continue;
                    }

                    PartitionTopicInfo partition;
                    if (_partitionsNeedingLeader.TryDequeue(out partition))
                    {
                        Logger.DebugFormat("Finding new leader for topic {0}, partition {1}", partition.Topic,
                                           partition.PartitionId);
                        Broker newLeader = null;
                        foreach (var broker in _brokers.Brokers)
                        {
                            var consumer = new Consumer(_config, broker.Value.Host, broker.Value.Port);
                            try
                            {
                                var metaData =
                                    consumer.GetMetaData(
                                                         TopicMetadataRequest.Create(new[] {partition.Topic}, 1, 0, clientId));
                                if (metaData != null && metaData.Any())
                                {
                                    var newPartitionData = metaData.First()
                                                                   .PartitionsMetadata
                                                                   .FirstOrDefault(p => p.PartitionId == partition.PartitionId);
                                    if (newPartitionData != null)
                                    {
                                        Logger.DebugFormat("New leader for {0} ({1}) is broker {2}", partition.Topic,
                                                           partition.PartitionId, newPartitionData.Leader.Id);
                                        newLeader = newPartitionData.Leader;
                                        break;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.WarnFormat("Error retrieving meta data from broker {0}: {1}", broker.Value.Id,
                                                  ex.FormatException());
                            }
                        }

                        if (newLeader == null)
                        {
                            Logger.ErrorFormat("New leader information could not be retrieved for {0} ({1})",
                                               partition.Topic, partition.PartitionId);
                        }
                        else
                        {
                            _createNewFetcher(partition, newLeader);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.ErrorFormat("PartitionLeaderFinder encountered an error: {0}", ex.FormatException());
                    Thread.Sleep(FailureRetryDelayMs);
                }
            }

            Logger.Info("Partition leader finder thread shutting down.");
        }

        public void Stop()
        {
            _stop = true;
        }
    }
}