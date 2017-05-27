using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using IFramework.Infrastructure.Logging;
using IFramework.IoC;
using Kafka.Client.Cluster;
using Kafka.Client.Utils;
using Kafka.Client.ZooKeeperIntegration.Events;

namespace Kafka.Client.ZooKeeperIntegration.Listeners
{
    /// <summary>
    ///     Listens to new broker registrations under a particular topic, in zookeeper and
    ///     keeps the related data structures updated
    /// </summary>
    internal class BrokerTopicsListener : IZooKeeperChildListener
    {
        public static ILogger Logger = IoCFactory.Resolve<ILoggerFactory>().Create(typeof(BrokerTopicsListener));

        private readonly IDictionary<int, Broker> actualBrokerIdMap;
        private readonly IDictionary<string, SortedSet<Partition>> actualBrokerTopicsPartitionsMap;
        private readonly Action<int, string, int> callback;
        private readonly object syncLock = new object();
        private readonly IZooKeeperClient zkclient;
        private IDictionary<int, Broker> oldBrokerIdMap;
        private IDictionary<string, SortedSet<Partition>> oldBrokerTopicsPartitionsMap;

        /// <summary>
        ///     Initializes a new instance of the <see cref="BrokerTopicsListener" /> class.
        /// </summary>
        /// <param name="zkclient">The wrapper on ZooKeeper client.</param>
        /// <param name="actualBrokerTopicsPartitionsMap">The actual broker topics partitions map.</param>
        /// <param name="actualBrokerIdMap">The actual broker id map.</param>
        /// <param name="callback">The callback invoked after new broker is added.</param>
        public BrokerTopicsListener(
            IZooKeeperClient zkclient,
            IDictionary<string, SortedSet<Partition>> actualBrokerTopicsPartitionsMap,
            IDictionary<int, Broker> actualBrokerIdMap,
            Action<int, string, int> callback)
        {
            this.zkclient = zkclient;
            this.actualBrokerTopicsPartitionsMap = actualBrokerTopicsPartitionsMap;
            this.actualBrokerIdMap = actualBrokerIdMap;
            this.callback = callback;
            oldBrokerIdMap = new Dictionary<int, Broker>(this.actualBrokerIdMap);
            oldBrokerTopicsPartitionsMap =
                new Dictionary<string, SortedSet<Partition>>(this.actualBrokerTopicsPartitionsMap);
            Logger.Debug("Creating broker topics listener to watch the following paths - \n"
                         + "/broker/topics, /broker/topics/topic, /broker/ids");
            Logger.Debug("Initialized this broker topics listener with initial mapping of broker id to "
                         + "partition id per topic with " + oldBrokerTopicsPartitionsMap.ToMultiString(
                             x => x.Key + " --> " + x.Value.ToMultiString(y => y.ToString(), ","), "; "));
        }

        /// <summary>
        ///     Called when the children of the given path changed
        /// </summary>
        /// <param name="e">
        ///     The <see cref="Kafka.Client.ZooKeeperIntegration.Events.ZooKeeperChildChangedEventArgs" /> instance containing the
        ///     event data
        ///     as parent path and children (null if parent was deleted).
        /// </param>
        public void HandleChildChange(ZooKeeperChildChangedEventArgs e)
        {
            Guard.NotNull(e, "e");
            Guard.NotNullNorEmpty(e.Path, "e.Path");
            Guard.NotNull(e.Children, "e.Children");

            lock (syncLock)
            {
                try
                {
                    var path = e.Path;
                    var childs = e.Children;
                    Logger.Debug("Watcher fired for path: " + path);
                    switch (path)
                    {
                        case ZooKeeperClient.DefaultBrokerTopicsPath:
                            var oldTopics = oldBrokerTopicsPartitionsMap.Keys.ToList();
                            var newTopics = childs.Except(oldTopics).ToList();
                            Logger.Debug("List of topics was changed at " + e.Path);
                            Logger.Debug("Current topics -> " + e.Children.ToMultiString(","));
                            Logger.Debug("Old list of topics -> " + oldTopics.ToMultiString(","));
                            Logger.Debug("List of newly registered topics -> " + newTopics.ToMultiString(","));
                            foreach (var newTopic in newTopics)
                            {
                                var brokerTopicPath = ZooKeeperClient.DefaultBrokerTopicsPath + "/" + newTopic;
                                var brokerList = zkclient.GetChildrenParentMayNotExist(brokerTopicPath);
                                ProcessNewBrokerInExistingTopic(newTopic, brokerList);
                                zkclient.Subscribe(ZooKeeperClient.DefaultBrokerTopicsPath + "/" + newTopic, this);
                            }
                            break;
                        case ZooKeeperClient.DefaultBrokerIdsPath:
                            Logger.Debug("List of brokers changed in the Kafka cluster " + e.Path);
                            Logger.Debug("Currently registered list of brokers -> " + e.Children.ToMultiString(","));
                            ProcessBrokerChange(path, childs);
                            break;
                        default:
                            var parts = path.Split('/');
                            var topic = parts.Last();
                            if (parts.Length == 4 && parts[2] == "topics" && childs != null)
                            {
                                Logger.Debug("List of brokers changed at " + path);
                                Logger.Debug(
                                    "Currently registered list of brokers for topic " + topic + " -> " +
                                    childs.ToMultiString(","));
                                ProcessNewBrokerInExistingTopic(topic, childs);
                            }
                            break;
                    }

                    oldBrokerTopicsPartitionsMap = actualBrokerTopicsPartitionsMap;
                    oldBrokerIdMap = actualBrokerIdMap;
                }
                catch (Exception exc)
                {
                    Logger.Debug("Error while handling " + e, exc);
                }
            }
        }

        /// <summary>
        ///     Resets the state of listener.
        /// </summary>
        public void ResetState()
        {
            Logger.Debug("Before reseting broker topic partitions state -> "
                         + oldBrokerTopicsPartitionsMap.ToMultiString(
                             x => x.Key + " --> " + x.Value.ToMultiString(y => y.ToString(), ","), "; "));
            oldBrokerTopicsPartitionsMap = actualBrokerTopicsPartitionsMap;
            Logger.Debug("After reseting broker topic partitions state -> "
                         + oldBrokerTopicsPartitionsMap.ToMultiString(
                             x => x.Key + " --> " + x.Value.ToMultiString(y => y.ToString(), ","), "; "));
            Logger.Debug("Before reseting broker id map state -> "
                         + oldBrokerIdMap.ToMultiString(", "));
            oldBrokerIdMap = actualBrokerIdMap;
            Logger.Debug("After reseting broker id map state -> "
                         + oldBrokerIdMap.ToMultiString(", "));
        }

        /// <summary>
        ///     Generate the updated mapping of (brokerId, numPartitions) for the new list of brokers
        ///     registered under some topic.
        /// </summary>
        /// <param name="topic">The path of the topic under which the brokers have changed..</param>
        /// <param name="childs">The list of changed brokers.</param>
        private void ProcessNewBrokerInExistingTopic(string topic, IEnumerable<string> childs)
        {
            if (oldBrokerTopicsPartitionsMap.ContainsKey(topic))
            {
                //Logger.Debug("Old list of brokers -> " + this.oldBrokerTopicsPartitionsMap[topic].ToMultiString(x => x.BrokerId.ToString(), ","));
            }

            var updatedBrokers = new SortedSet<int>(childs.Select(x => int.Parse(x, CultureInfo.InvariantCulture)));
            var brokerTopicPath = ZooKeeperClient.DefaultBrokerTopicsPath + "/" + topic;
            var sortedBrokerPartitions = new SortedDictionary<int, int>();
            foreach (var bid in updatedBrokers)
            {
                var num = zkclient.ReadData<string>(brokerTopicPath + "/" + bid);
                sortedBrokerPartitions.Add(bid, int.Parse(num, CultureInfo.InvariantCulture));
            }

            var updatedBrokerParts = new SortedSet<Partition>();
            foreach (var bp in sortedBrokerPartitions)
                for (var i = 0; i < bp.Value; i++)
                {
                    //var bidPid = new Partition(bp.Key, i);
                    //updatedBrokerParts.Add(bidPid);
                }

            Logger.Debug(
                "Currently registered list of brokers for topic " + topic + " -> " + childs.ToMultiString(", "));
            var mergedBrokerParts = updatedBrokerParts;
            if (actualBrokerTopicsPartitionsMap.ContainsKey(topic))
            {
                var oldBrokerParts = actualBrokerTopicsPartitionsMap[topic];
                Logger.Debug(
                    "Unregistered list of brokers for topic " + topic + " -> " + oldBrokerParts.ToMultiString(", "));
                foreach (var oldBrokerPart in oldBrokerParts)
                    mergedBrokerParts.Add(oldBrokerPart);
            }
            else
            {
                actualBrokerTopicsPartitionsMap.Add(topic, null);
            }

            //this.actualBrokerTopicsPartitionsMap[topic] = new SortedSet<Partition>(mergedBrokerParts.Where(x => this.actualBrokerIdMap.ContainsKey(x.BrokerId)));
        }

        /// <summary>
        ///     Processes change in the broker lists.
        /// </summary>
        /// <param name="path">The parent path of brokers list.</param>
        /// <param name="childs">The current brokers.</param>
        private void ProcessBrokerChange(string path, IEnumerable<string> childs)
        {
            if (path != ZooKeeperClient.DefaultBrokerIdsPath)
                return;

            var updatedBrokers = childs.Select(x => int.Parse(x, CultureInfo.InvariantCulture)).ToList();
            var oldBrokers = oldBrokerIdMap.Select(x => x.Key).ToList();
            var newBrokers = updatedBrokers.Except(oldBrokers).ToList();
            Logger.Debug("List of newly registered brokers -> " + newBrokers.ToMultiString(","));
            foreach (var bid in newBrokers)
            {
                var brokerInfo = zkclient.ReadData<string>(ZooKeeperClient.DefaultBrokerIdsPath + "/" + bid);
                var brokerHost = brokerInfo.Split(':');
                var port = int.Parse(brokerHost[1], CultureInfo.InvariantCulture);
                actualBrokerIdMap.Add(bid, new Broker(bid, brokerHost[0], port));
                if (callback != null)
                {
                    Logger.Debug("Invoking the callback for broker: " + bid);
                    callback(bid, brokerHost[1], port);
                }
            }

            var deadBrokers = oldBrokers.Except(updatedBrokers).ToList();
            Logger.Debug("Deleting broker ids for dead brokers -> " + deadBrokers.ToMultiString(","));
            foreach (var bid in deadBrokers)
            {
                Logger.Debug("Deleting dead broker: " + bid);
                actualBrokerIdMap.Remove(bid);
                foreach (var topicMap in actualBrokerTopicsPartitionsMap)
                {
                    //int affected = topicMap.Value.RemoveWhere(x => x.BrokerId == bid);
                    //if (affected > 0)
                    //{
                    //    Logger.Debug("Removing dead broker " + bid + " for topic: " + topicMap.Key);
                    //    Logger.Debug("Actual list of mapped brokers is -> " + topicMap.Value.ToMultiString(x => x.ToString(), ","));
                    //}
                }
            }
        }
    }
}