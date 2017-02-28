using Kafka.Client.Cfg;
using Kafka.Client.Consumers;
using Kafka.Client.Helper;
using Kafka.Client.Requests;
using System.Collections.Generic;
using IFramework.Infrastructure;
using System.Linq;
using KafkaMessages = Kafka.Client.Messages;
using Kafka.Client.Serialization;
using System.Threading;
using IFramework.Message;
using System.Collections.Concurrent;
using IFramework.Message.Impl;
using IFramework.Config;
using IFramework.Infrastructure.Logging;
using IFramework.IoC;
using IFramework.MessageQueue.MSKafka.MessageFormat;
using System;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.MessageQueue.MSKafka
{
    public delegate void OnKafkaMessageReceived(KafkaConsumer consumer, KafkaMessages.Message message);

    public class KafkaConsumer : ICommitOffsetable
    {
        public string ZkConnectionString { get; protected set; }
        public string Topic { get; protected set; }
        public string GroupId { get; protected set; }
        public string ConsumerId { get; protected set; }
        public ZookeeperConsumerConnector ZkConsumerConnector { get; protected set; }
        public ConsumerConfiguration ConsumerConfiguration { get; protected set; }
        public ConcurrentDictionary<int, SlidingDoor> SlidingDoors { get; protected set; }
        protected int _fullLoadThreshold;
        protected int _waitInterval;

        protected OnKafkaMessageReceived _onMessageReceived;
        protected CancellationTokenSource _cancellationTokenSource;
        protected Task _consumerTask;

        protected ILogger _logger = IoCFactory.Resolve<ILoggerFactory>().Create(typeof(KafkaConsumer).Name);

        public string Id
        {
            get
            {
                return $"{GroupId}.{Topic}.{ConsumerId}";
            }
        }
        public KafkaConsumer(string zkConnectionString, string topic, string groupId, string consumerId,
                             OnKafkaMessageReceived onMessageReceived,
                             int backOffIncrement = 30, int fullLoadThreshold = 1000, int waitInterval = 1000,
                             bool start = true)
        {
            _fullLoadThreshold = fullLoadThreshold;
            _waitInterval = waitInterval;
            ZkConnectionString = zkConnectionString;
            Topic = topic;
            GroupId = groupId;
            ConsumerId = consumerId ?? string.Empty;
            SlidingDoors = new ConcurrentDictionary<int, SlidingDoor>();
            ConsumerConfiguration = new ConsumerConfiguration
            {
                BackOffIncrement = backOffIncrement,
                AutoCommit = false,
                GroupId = GroupId,
                ConsumerId = ConsumerId,
                BufferSize = ConsumerConfiguration.DefaultBufferSize,
                MaxFetchBufferLength = ConsumerConfiguration.DefaultMaxFetchBufferLength,
                FetchSize = ConsumerConfiguration.DefaultFetchSize,
                AutoOffsetReset = OffsetRequest.LargestTime,
                ZooKeeper = KafkaClient.GetZooKeeperConfiguration(zkConnectionString),
                ShutdownTimeout = 100
            };
            _onMessageReceived = onMessageReceived;
            if (start)
            {
                Start();
            }
        }

        private void ZkDisconnectedHandler(object sender, EventArgs args)
        {
            _logger.Error($"{GroupId}.{ConsumerId} zookeeper disconnected!");
            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                ReStart();
            }
        }

        private void ZkRebalanceHandler(object sender, EventArgs args)
        {
            _logger.Error($"{GroupId}.{ConsumerId} zookeeper RebalanceHandler!");
        }
        private void ZkExpiredHandler(object sender, EventArgs args)
        {
            _logger.Error($"{GroupId}.{ConsumerId} zookeeper ZkExpiredHandler!");
        }

        protected void ReStart()
        {
            Stop();
            Start();
        }

        public void Start()
        {
            ZkConsumerConnector = new ZookeeperConsumerConnector(ConsumerConfiguration, true,
                                                                 rebalanceHandler: ZkRebalanceHandler,
                                                                 zkDisconnectedHandler: ZkDisconnectedHandler,
                                                                 zkExpiredHandler: ZkExpiredHandler);
            _cancellationTokenSource = new CancellationTokenSource();
            _consumerTask = Task.Factory.StartNew((cs) => ReceiveMessages(cs as CancellationTokenSource,
                                                                          _onMessageReceived),
                                                     _cancellationTokenSource,
                                                     _cancellationTokenSource.Token,
                                                     TaskCreationOptions.LongRunning,
                                                     TaskScheduler.Default);
        }

        IDictionary<string, IList<KafkaMessageStream<KafkaMessages.Message>>> _streams;
        public IKafkaMessageStream<KafkaMessages.Message> GetStream()
        {
            var topicDic = new Dictionary<string, int>() {
                        {Topic, 1 }
                    };
            _streams = _streams ?? ZkConsumerConnector.CreateMessageStreams(topicDic, new DefaultDecoder());
            var stream = _streams[Topic][0];
            _logger.Debug($"consumer {ConsumerId} has got Stream");
            return stream;
        }

        void ReceiveMessages(CancellationTokenSource cancellationTokenSource, OnKafkaMessageReceived onMessagesReceived)
        {
            IEnumerable<KafkaMessages.Message> messages = null;

            #region peek messages that not been consumed since last time
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    //var linkedTimeoutCTS = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenSource.Token,
                    //                                                                       new CancellationTokenSource(3000).Token);
                    messages = GetMessages(cancellationTokenSource.Token);
                    foreach (var message in messages)
                    {
                        try
                        {
                            AddMessage(message);
                            onMessagesReceived(this, message);
                            BlockIfFullLoad();
                        }
                        catch (OperationCanceledException)
                        {
                            return;
                        }
                        catch (ThreadAbortException)
                        {
                            return;
                        }
                        catch (Exception ex)
                        {
                            if (message.Payload != null)
                            {
                                RemoveMessage(message.PartitionId.Value, message.Offset);
                            }
                            _logger.Error(ex.GetBaseException().Message, ex);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (ThreadAbortException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    if (!cancellationTokenSource.IsCancellationRequested)
                    {
                        Thread.Sleep(1000);
                        _logger.Error(ex.GetBaseException().Message, ex);
                    }
                }
            }
            #endregion
        }



        protected void BlockIfFullLoad()
        {
            while (SlidingDoors.Sum(d => d.Value.MessageCount) > _fullLoadThreshold)
            {
                Thread.Sleep(_waitInterval);
                _logger.Warn($"working is full load sleep 1000 ms");
            }
        }

        protected void AddMessage(KafkaMessages.Message message)
        {
            var slidingDoor = SlidingDoors.GetOrAdd(message.PartitionId.Value, partition =>
            {
                return new SlidingDoor(CommitOffset,
                                       string.Empty,
                                       partition,
                                       Configuration.Instance.GetCommitPerMessage());
            });
            slidingDoor.AddOffset(message.Offset);
        }

        internal void RemoveMessage(int partition, long offset)
        {
            var slidingDoor = SlidingDoors.TryGetValue(partition);
            if (slidingDoor == null)
            {
                throw new System.Exception("partition slidingDoor not exists");
            }
            slidingDoor.RemoveOffset(offset);
        }

        public IEnumerable<KafkaMessages.Message> GetMessages(CancellationToken cancellationToken)
        {
            return GetStream().GetCancellable(cancellationToken);
        }

        public void CommitOffset(IMessageContext messageContext)
        {
            var message = (messageContext as MessageContext);
            RemoveMessage(message.Partition, message.Offset);
        }

        public void CommitOffset(int partition, long offset)
        {
            // kafka not use broker in cluster mode
            ZkConsumerConnector.CommitOffset(Topic, partition, offset, false);
        }

        public void CommitOffset(string broker, int partition, long offset)
        {
            // kafka not use broker in cluster mode
            CommitOffset(partition, offset);
        }

        public void Stop()
        {
            _cancellationTokenSource?.Cancel(true);
            _consumerTask?.Wait();
            _consumerTask?.Dispose();
            _consumerTask = null;
            _cancellationTokenSource = null;
            _streams = null;
            SlidingDoors.Clear();
            if (ZkConsumerConnector != null)
            {
                ZkConsumerConnector.Dispose();
                ZkConsumerConnector = null;
            }
        }
    }
}