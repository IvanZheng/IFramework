
using IFramework.Config;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Logging;
using IFramework.IoC;
using IFramework.Message;
using IFramework.MessageQueue.EQueue.MessageFormat;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using EQueueMessages = EQueue.Protocols;
using EQueueConsumers = EQueue.Clients.Consumers;
using System.Net;

namespace IFramework.MessageQueue.EQueue
{
    public class EQueueConsumer
    {
        public string BrokerAddress { get; protected set; }
        public int ConsumerPort { get; protected set; }
        public int AdminPort { get; protected set; }
        public string Topic { get; protected set; }
        public string GroupId { get; protected set; }
        public string ConsumerId { get; protected set; }
        public ConcurrentDictionary<int, SlidingDoor> SlidingDoors { get; protected set; }
        protected EQueueConsumers.Consumer Consumer { get; set; }
        protected int _fullLoadThreshold;
        protected int _waitInterval;
        protected ILogger _logger = IoCFactory.Resolve<ILoggerFactory>().Create(typeof(EQueueConsumer).Name);
        public EQueueConsumer(string brokerAdress, int consumerPort, int adminPort, string topic, string groupId, string consumerId, int fullLoadThreshold = 1000, int waitInterval = 1000)
        {
            BrokerAddress = brokerAdress;
            ConsumerPort = consumerPort;
            AdminPort = adminPort;
            _fullLoadThreshold = fullLoadThreshold;
            _waitInterval = waitInterval;
            SlidingDoors = new ConcurrentDictionary<int, SlidingDoor>();
            Topic = topic;
            GroupId = groupId;
            ConsumerId = consumerId ?? string.Empty;
        }

        public void Start()
        {
            var setting = new EQueueConsumers.ConsumerSetting
            {
                AutoPull = false,
                ConsumeFromWhere = EQueueMessages.ConsumeFromWhere.FirstOffset,
                BrokerAddress = new IPEndPoint(IPAddress.Parse(BrokerAddress), ConsumerPort),
                BrokerAdminAddress = new IPEndPoint(IPAddress.Parse(BrokerAddress), AdminPort)
            };
            Consumer = new EQueueConsumers.Consumer(GroupId, setting)
                                          .Subscribe(Topic)
                                          .Start();
        }

        public void Stop()
        {
            Consumer?.Shutdown();
        }

        public void BlockIfFullLoad()
        {
            while (SlidingDoors.Sum(d => d.Value.MessageCount) > _fullLoadThreshold)
            {
                Thread.Sleep(_waitInterval);
                _logger.Warn($"working is full load sleep 1000 ms");
            }
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
            {
                throw new Exception("partition slidingDoor not exists");
            }
            slidingDoor.RemoveOffset(offset);
        }

        internal void CommitOffset(IMessageContext messageContext)
        {
            var message = (messageContext as MessageContext);
            RemoveMessage(message.Partition, message.Offset);
        }

        public IEnumerable<EQueueMessages.QueueMessage> PullMessages(int maxCount, int timeoutMilliseconds, CancellationToken cancellationToken)
        {
            return Consumer.PullMessages(maxCount, timeoutMilliseconds, cancellationToken);
        }


        public void CommitOffset(string broker, int partition, long offset)
        {
            Consumer.CommitConsumeOffset(broker, Topic, partition, offset);
        }

    }
}
