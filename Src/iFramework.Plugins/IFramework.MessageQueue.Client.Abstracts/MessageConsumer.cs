using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.Infrastructure;
using IFramework.Message;
using Microsoft.Extensions.Logging;

namespace IFramework.MessageQueue.Client.Abstracts
{
    public abstract class MessageConsumer : IMessageConsumer
    {
        protected CancellationTokenSource CancellationTokenSource;
        protected ConsumerConfig ConsumerConfig;
        protected Task ConsumerTask;
        protected ILogger Logger;

        protected MessageConsumer(string brokerList,
                                  string topic,
                                  string groupId,
                                  string consumerId,
                                  ConsumerConfig consumerConfig = null)
        {
            Logger = IoCFactory.GetService<ILoggerFactory>().CreateLogger(GetType().Name);
            ConsumerConfig = consumerConfig ?? ConsumerConfig.DefaultConfig;
            BrokerList = brokerList;
            Topic = topic;
            GroupId = groupId;
            ConsumerId = consumerId ?? string.Empty;
            SlidingDoors = new ConcurrentDictionary<int, SlidingDoor>();
        }

        public ConcurrentDictionary<int, SlidingDoor> SlidingDoors { get; protected set; }
        public string BrokerList { get; protected set; }
        public string Topic { get; protected set; }
        public string GroupId { get; protected set; }
        public string ConsumerId { get; protected set; }
        public string Id => $"{GroupId}.{Topic}.{ConsumerId}";

        public virtual void Start()
        {
            CancellationTokenSource = new CancellationTokenSource();
            ConsumerTask = Task.Factory.StartNew(cs => ReceiveMessages(cs as CancellationTokenSource),
                                                 CancellationTokenSource,
                                                 CancellationTokenSource.Token,
                                                 TaskCreationOptions.LongRunning,
                                                 TaskScheduler.Default);
        }

        public virtual void Stop()
        {
            CancellationTokenSource?.Cancel(true);
            ConsumerTask?.Wait();
            ConsumerTask?.Dispose();
            SlidingDoors.Clear();
            ConsumerTask = null;
            CancellationTokenSource = null;
        }

        public virtual void CommitOffset(IMessageContext messageContext)
        {
            FinishConsumingMessage(messageContext.MessageOffset);
        }

        protected abstract void PollMessages();


        protected virtual void ReceiveMessages(CancellationTokenSource cancellationTokenSource)
        {
            #region peek messages that not been consumed since last time

            Logger.LogDebug($"{Id} ReceiveMessages start");
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    PollMessages();
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
                    if (!cancellationTokenSource.IsCancellationRequested)
                    {
                        Task.Delay(1000).Wait();
                        Logger.LogError(ex, $"{Id} ReceiveMessages failed!");
                    }
                }
            }

            #endregion
        }

        protected void BlockIfFullLoad()
        {
            while (SlidingDoors.Sum(d => d.Value.MessageCount) > ConsumerConfig.FullLoadThreshold)
            {
                Task.Delay(ConsumerConfig.WaitInterval).Wait();
                Logger.LogWarning($"{Id} is full load sleep 1000 ms");
            }
        }

        

        protected void AddMessageOffset(int partition, long offset)
        {
            var slidingDoor = SlidingDoors.GetOrAdd(partition,
                                                    key => new SlidingDoor(CommitOffset,
                                                                           key,
                                                                           Configuration.Instance.GetCommitPerMessage()));
            slidingDoor.AddOffset(offset);
        }

        /// <summary>
        /// </summary>
        /// <param name="messageOffset"></param>
        protected virtual void FinishConsumingMessage(MessageOffset messageOffset)
        {
            var slidingDoor = SlidingDoors.TryGetValue(messageOffset.Partition);
            if (slidingDoor == null)
            {
                throw new Exception("partition slidingDoor not exists");
            }
            slidingDoor.RemoveOffset(messageOffset);
        }

        private void CommitOffset(MessageOffset messageOffset)
        {
            CommitOffsetAsync(messageOffset.Broker,
                              messageOffset.Partition,
                              messageOffset.Offset);
        }

        public abstract Task CommitOffsetAsync(string broker, int partition, long offset);
    }
}