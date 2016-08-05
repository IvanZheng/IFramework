using Kafka.Client.Cfg;
using Kafka.Client.Consumers;
using Kafka.Client.Helper;
using Kafka.Client.Requests;
using System.Collections.Generic;
using IFramework.Infrastructure;
using System.Linq;
using KafkaMessages = Kafka.Client.Messages;
using Kafka.Client.Serialization;

namespace IFramework.MessageQueue.MSKafka
{
    public class KafkaConsumer
    {
        public string ZkConnectionString { get; protected set; }
        public string Topic { get; protected set; }
        public int Partition { get; protected set; }
        public string GroupId { get; protected set; }
        public string ConsumerId { get; protected set; }
        public long FetchedOffset { get; protected set; }
        public ZookeeperConsumerConnector ZkConsumerConnector { get; protected set; }
        public ConsumerConfiguration ConsumerConfiguration { get; protected set; }
        public KafkaSimpleManager<int, KafkaMessages.Message> KafkaSimpleManager { get; protected set; }
        public KafkaConsumer(string zkConnectionString, string topic, int partition, string groupId, string consumerId = null)
        {
            FetchedOffset = 0;
            ZkConnectionString = zkConnectionString;
            Topic = topic;
            Partition = partition;
            GroupId = groupId;
            ConsumerId = consumerId ?? this.GetType().Name;
            FetchedOffset = -1;
            ConsumerConfiguration = new ConsumerConfiguration
            {
                AutoCommit = false,
                GroupId = GroupId,
                ConsumerId = ConsumerId,
                MaxFetchBufferLength = KafkaSimpleManagerConfiguration.DefaultBufferSize,
                FetchSize = KafkaSimpleManagerConfiguration.DefaultFetchSize,
                AutoOffsetReset = OffsetRequest.LargestTime,
                NumberOfTries = 3,
                ZooKeeper = new ZooKeeperConfiguration(zkConnectionString, 3000, 3000, 1000)
            };
            ZkConsumerConnector = new ZookeeperConsumerConnector(ConsumerConfiguration, true);
            var fetchedOffsets = ZkConsumerConnector.GetOffset(Topic);

            FetchedOffset = fetchedOffsets.TryGetValue(Partition, 0);
            if (FetchedOffset > 0)
            {
                FetchedOffset++;
            }
        }

        Consumer _consumer;
        public Consumer Consumer
        {
            get
            {
                if (_consumer == null)
                {
                    // create the Consumer higher level manager
                    var managerConfig = new KafkaSimpleManagerConfiguration()
                    {
                        FetchSize = KafkaSimpleManagerConfiguration.DefaultFetchSize,
                        BufferSize = KafkaSimpleManagerConfiguration.DefaultBufferSize,
                        Zookeeper = ZkConnectionString
                    };
                    KafkaSimpleManager = new KafkaSimpleManager<int, KafkaMessages.Message>(managerConfig);
                    // get all available partitions for a topic through the manager
                    //var allPartitions = consumerManager.GetTopicPartitionsFromZK(Topic);
                    // Refresh metadata and grab a consumer for desired partitions
                    KafkaSimpleManager.RefreshMetadata(0, ConsumerId, 0, Topic, true);
                    _consumer = KafkaSimpleManager.GetConsumer(Topic, Partition);
                    var topicDic = new Dictionary<string, int>() {
                        {Topic, 1 }
                    };
                    ZkConsumerConnector.CreateMessageStreams(topicDic, new DefaultDecoder());
                }
                return _consumer;
            }
        }
        internal IEnumerable<KafkaMessages.Message> PeekBatch(int fetchSize = KafkaSimpleManagerConfiguration.DefaultFetchSize,
                                                int maxWaitTime = 2000, int minBytes = 1)
        {
            //var fetchRequest = new FetchRequest(0, ConsumerId, maxWaitTime, minBytes, new Dictionary<string, List<PartitionFetchInfo>> {
            //    { Topic, new List<PartitionFetchInfo> { new PartitionFetchInfo(Partition, FetchedOffset, fetchSize)}  }
            //});
            var messages = Consumer.Fetch(ConsumerId, Topic, 0, Partition, FetchedOffset, fetchSize, maxWaitTime, minBytes)
                                   .MessageSet(Topic, Partition)
                                   .Select(mo => mo.Message)
                                   .ToList();
            if (messages.Count > 0)
            {
                FetchedOffset = messages.Last().Offset + 1;
            }
            return messages;
        }

        internal void CommitOffset(long offset)
        {
            ZkConsumerConnector.CommitOffset(Topic, Partition, offset, false);
        }

        internal void Stop()
        {
            if (_consumer != null)
            {
                _consumer.Dispose();
                _consumer = null;
            }
            if (ZkConsumerConnector != null)
            {
                ZkConsumerConnector.Dispose();
                ZkConsumerConnector = null;
            }
            if (KafkaSimpleManager != null)
            {
                KafkaSimpleManager.Dispose();
                KafkaSimpleManager = null;
            }
        }
    }
}
