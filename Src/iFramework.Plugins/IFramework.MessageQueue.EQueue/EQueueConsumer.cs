using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using IFramework.Config;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Logging;
using IFramework.IoC;
using IFramework.Message;
using IFramework.MessageQueue.EQueue.MessageFormat;
using EQueueMessages = EQueue.Protocols;
using EQueueConsumers = EQueue.Clients.Consumers;

namespace IFramework.MessageQueue.EQueue
{
    public delegate void OnEQueueMessageReceived(EQueueConsumer consumer, EQueueMessages.QueueMessage message);

    public class EQueueConsumer : ICommitOffsetable
    {
        protected CancellationTokenSource _cancellationTokenSource;
        protected Task _consumerTask;
        protected int _fullLoadThreshold;
        protected ILogger _logger = IoCFactory.Resolve<ILoggerFactory>().Create(typeof(EQueueConsumer).Name);
        private readonly OnEQueueMessageReceived _onMessageReceived;
        protected int _waitInterval;

        public EQueueConsumer(string clusterName, List<IPEndPoint> nameServerList,
            string topic, string groupId, string consumerId,
            OnEQueueMessageReceived onMessageReceived,
            int fullLoadThreshold = 1000, int waitInterval = 1000,
            bool start = true)
        {
            _onMessageReceived = onMessageReceived;
            ClusterName = clusterName;
            NameServerList = nameServerList;
            _fullLoadThreshold = fullLoadThreshold;
            _waitInterval = waitInterval;
            SlidingDoors = new ConcurrentDictionary<int, SlidingDoor>();
            Topic = topic;
            GroupId = groupId;
            ConsumerId = consumerId ?? string.Empty;
            if (start)
                Start();
        }

        public string ClusterName { get; protected set; }
        public List<IPEndPoint> NameServerList { get; protected set; }
        public string Topic { get; protected set; }
        public string GroupId { get; protected set; }
        public string ConsumerId { get; protected set; }
        public ConcurrentDictionary<int, SlidingDoor> SlidingDoors { get; protected set; }
        protected EQueueConsumers.Consumer Consumer { get; set; }

        public string Id => $"{GroupId}.{Topic}.{ConsumerId}";

        public void Start()
        {
            var setting = new EQueueConsumers.ConsumerSetting
            {
                AutoPull = false,
                ConsumeFromWhere = EQueueMessages.ConsumeFromWhere.FirstOffset,
                ClusterName = ClusterName,
                NameServerList = NameServerList
            };
            Consumer = new EQueueConsumers.Consumer(GroupId, setting)
                .Subscribe(Topic)
                .Start();
            _cancellationTokenSource = new CancellationTokenSource();
            _consumerTask = Task.Factory.StartNew(cs => ReceiveMessages(cs as CancellationTokenSource,
                    _onMessageReceived),
                _cancellationTokenSource,
                _cancellationTokenSource.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        public void Stop()
        {
            Consumer?.Stop();
            _cancellationTokenSource.Cancel(true);
            _consumerTask.Wait(5000);
        }

        public void CommitOffset(IMessageContext messageContext)
        {
            var message = messageContext as MessageContext;
            RemoveMessage(message.Partition, message.Offset);
        }

        public void BlockIfFullLoad()
        {
            while (SlidingDoors.Sum(d => d.Value.MessageCount) > _fullLoadThreshold)
            {
                Thread.Sleep(_waitInterval);
                _logger.Warn($"working is full load sleep 1000 ms");
            }
        }

        private void ReceiveMessages(CancellationTokenSource cancellationTokenSource,
            OnEQueueMessageReceived onMessageReceived)
        {
            IEnumerable<EQueueMessages.QueueMessage> messages = null;

            #region peek messages that not been consumed since last time

            while (!cancellationTokenSource.IsCancellationRequested)
                try
                {
                    messages = PullMessages(100, 2000, cancellationTokenSource.Token);
                    foreach (var message in messages)
                        try
                        {
                            AddMessage(message);
                            onMessageReceived(this, message);
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
                            if (message.Body != null)
                                RemoveMessage(message.QueueId, message.QueueOffset);
                            _logger.Error(ex.GetBaseException().Message, ex);
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

            #endregion
        }

        internal void AddMessage(EQueueMessages.QueueMessage message)
        {
            var slidingDoor = SlidingDoors.GetOrAdd(message.QueueId, partition =>
            {
                return new SlidingDoor(CommitOffset,
                    message.BrokerName,
                    partition,
                    Configuration.Instance.GetCommitPerMessage());
            });
            slidingDoor.AddOffset(message.QueueOffset);
        }

        internal void RemoveMessage(int partition, long offset)
        {
            var slidingDoor = SlidingDoors.TryGetValue(partition);
            if (slidingDoor == null)
                throw new Exception("partition slidingDoor not exists");
            slidingDoor.RemoveOffset(offset);
        }

        public IEnumerable<EQueueMessages.QueueMessage> PullMessages(int maxCount, int timeoutMilliseconds,
            CancellationToken cancellationToken)
        {
            return Consumer.PullMessages(maxCount, timeoutMilliseconds, cancellationToken);
        }


        public void CommitOffset(string broker, int partition, long offset)
        {
            Consumer.CommitConsumeOffset(broker, Topic, partition, offset);
        }
    }
}