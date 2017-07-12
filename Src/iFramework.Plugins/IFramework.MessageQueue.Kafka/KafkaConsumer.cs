using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IFramework.Config;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Logging;
using IFramework.IoC;
using IFramework.Message;
using IFramework.MessageQueue.MSKafka.MessageFormat;
using Kafka.Client.Cfg;
using Kafka.Client.Consumers;
using Kafka.Client.Requests;
using Kafka.Client.Serialization;
using KafkaMessages = Kafka.Client.Messages;

namespace IFramework.MessageQueue.MSKafka
{
    public delegate void OnKafkaMessageReceived(KafkaConsumer consumer, KafkaMessages.Message message);

    public class KafkaConsumer: ICommitOffsetable
    {
        protected CancellationTokenSource _cancellationTokenSource;
        protected Task _consumerTask;
        protected ILogger _logger = IoCFactory.Resolve<ILoggerFactory>().Create(typeof(KafkaConsumer).Name);
        protected OnKafkaMessageReceived _onMessageReceived;
        private IDictionary<string, IList<KafkaMessageStream<KafkaMessages.Message>>> _streams;
        protected ConsumerConfig _consumerConfig;



        public KafkaConsumer(string zkConnectionString,
                             string topic,
                             string groupId,
                             string consumerId,
                             OnKafkaMessageReceived onMessageReceived,
                             ConsumerConfig consumerConfig = null,
                             bool start = true)
        {
            _consumerConfig = consumerConfig ?? ConsumerConfig.DefaultConfig;
             ZkConnectionString = zkConnectionString;
            Topic = topic;
            GroupId = groupId;
            ConsumerId = consumerId ?? string.Empty;
            SlidingDoors = new ConcurrentDictionary<int, SlidingDoor>();
            ConsumerConfiguration = new ConsumerConfiguration
            {
                BackOffIncrement = _consumerConfig.BackOffIncrement,
                AutoCommit = false,
                GroupId = GroupId,
                ConsumerId = ConsumerId,
                BufferSize = ConsumerConfiguration.DefaultBufferSize,
                MaxFetchBufferLength = ConsumerConfiguration.DefaultMaxFetchBufferLength,
                FetchSize = ConsumerConfiguration.DefaultFetchSize,
                AutoOffsetReset = _consumerConfig.AutoOffsetReset,
                ZooKeeper = KafkaClient.GetZooKeeperConfiguration(zkConnectionString),
                ShutdownTimeout = 100
            };
            _onMessageReceived = onMessageReceived;
            if (start)
            {
                Start();
            }
        }

        public string ZkConnectionString { get; protected set; }
        public string Topic { get; protected set; }
        public string GroupId { get; protected set; }
        public string ConsumerId { get; protected set; }
        public ZookeeperConsumerConnector ZkConsumerConnector { get; protected set; }
        public ConsumerConfiguration ConsumerConfiguration { get; protected set; }
        public ConcurrentDictionary<int, SlidingDoor> SlidingDoors { get; protected set; }

        public string Id => $"{GroupId}.{Topic}.{ConsumerId}";

        public void Start()
        {
            ZkConsumerConnector = new ZookeeperConsumerConnector(ConsumerConfiguration, true,
                                                                 ZkRebalanceHandler,
                                                                 ZkDisconnectedHandler,
                                                                 ZkExpiredHandler);
            _cancellationTokenSource = new CancellationTokenSource();
            _consumerTask = Task.Factory.StartNew(cs => ReceiveMessages(cs as CancellationTokenSource,
                                                                        _onMessageReceived),
                                                  _cancellationTokenSource,
                                                  _cancellationTokenSource.Token,
                                                  TaskCreationOptions.LongRunning,
                                                  TaskScheduler.Default);
        }

        public void CommitOffset(IMessageContext messageContext)
        {
            var message = messageContext as MessageContext;
            RemoveMessage(message.Partition, message.Offset);
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

        public IKafkaMessageStream<KafkaMessages.Message> GetStream()
        {
            var topicDic = new Dictionary<string, int>
            {
                {Topic, 1}
            };
            _streams = _streams ?? ZkConsumerConnector.CreateMessageStreams(topicDic, new DefaultDecoder());
            var stream = _streams[Topic][0];
            _logger.Debug($"consumer {ConsumerId} has got Stream");
            return stream;
        }

        private void ReceiveMessages(CancellationTokenSource cancellationTokenSource,
                                     OnKafkaMessageReceived onMessagesReceived)
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
            while (SlidingDoors.Sum(d => d.Value.MessageCount) > _consumerConfig.FullLoadThreshold)
            {
                Thread.Sleep(_consumerConfig.WaitInterval);
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
                throw new Exception("partition slidingDoor not exists");
            }
            slidingDoor.RemoveOffset(offset);
        }

        public IEnumerable<KafkaMessages.Message> GetMessages(CancellationToken cancellationToken)
        {
            return GetStream().GetCancellable(cancellationToken);
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
    }
}