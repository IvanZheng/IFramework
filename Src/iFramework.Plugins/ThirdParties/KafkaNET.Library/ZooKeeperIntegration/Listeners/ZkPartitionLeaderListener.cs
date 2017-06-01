using System.Collections.Generic;
using IFramework.Infrastructure.Logging;
using IFramework.IoC;
using Kafka.Client.Utils;
using Kafka.Client.ZooKeeperIntegration.Events;

namespace Kafka.Client.ZooKeeperIntegration.Listeners
{
    internal class ZkPartitionLeaderListener<TData> : IZooKeeperDataListener
    {
        public static ILogger Logger = IoCFactory.Resolve<ILoggerFactory>()
                                                 .Create(typeof(ZkPartitionLeaderListener<TData>));

        private readonly Dictionary<string, int> _partitionLeaderMap;

        private readonly ZKRebalancerListener<TData> _rebalancer;

        public ZkPartitionLeaderListener(ZKRebalancerListener<TData> rebalancer,
                                         Dictionary<string, int> partitionLeaderMap = null)
        {
            _rebalancer = rebalancer;
            if (partitionLeaderMap != null)
            {
                _partitionLeaderMap = new Dictionary<string, int>(partitionLeaderMap);
            }
            else
            {
                _partitionLeaderMap = new Dictionary<string, int>();
            }
        }

        public void HandleDataChange(ZooKeeperDataChangedEventArgs args)
        {
            int parsedLeader;
            var nodePath = args.Path;
            var nodeData = args.Data;

            Logger.Info("A partition leader or ISR list has been updated. Determining if rebalancing is necessary");
            if (!ZkUtils.TryParsePartitionLeader(nodeData, out parsedLeader))
            {
                Logger.Error("Skipping rebalancing. Failed to parse partition leader for path: " + nodePath +
                             " from ZK node");
            }
            else
            {
                if (!_partitionLeaderMap.ContainsKey(nodePath) || _partitionLeaderMap[nodePath] != parsedLeader)
                {
                    var currentLeader = _partitionLeaderMap.ContainsKey(nodePath)
                                            ? _partitionLeaderMap[nodePath].ToString()
                                            : "null";
                    Logger.Info("Performing rebalancing. Leader value for path: " + nodePath + " has changed from " +
                                currentLeader + " to " + parsedLeader);
                    _partitionLeaderMap[nodePath] = parsedLeader;
                    _rebalancer.AsyncRebalance();
                }
                else
                {
                    Logger.Info("Skipping rebalancing. Leader value for path: " + nodePath + " is " + parsedLeader +
                                " and has not changed");
                }
            }
        }

        public void HandleDataDelete(ZooKeeperDataChangedEventArgs args)
        {
            _rebalancer.AsyncRebalance();
        }
    }
}