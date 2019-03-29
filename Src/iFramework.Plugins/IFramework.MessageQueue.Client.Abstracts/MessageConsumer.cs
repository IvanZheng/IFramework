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
        protected MessageConsumer(string[] topics,
                                  string groupId,
                                  string consumerId,
                                  ConsumerConfig consumerConfig = null)
        {
            Logger = ObjectProviderFactory.GetService<ILoggerFactory>().CreateLogger(GetType());
            ConsumerConfig = consumerConfig ?? ConsumerConfig.DefaultConfig;
            Topics = topics;
            GroupId = groupId;
            ConsumerId = consumerId ?? string.Empty;
            SlidingDoors = new ConcurrentDictionary<string, SlidingDoor>();
        }

        public ConcurrentDictionary<string, SlidingDoor> SlidingDoors { get; protected set; }
        public string[] Topics { get; protected set; }
        public string GroupId { get; protected set; }
        public string ConsumerId { get; protected set; }
        public string Id => $"{GroupId}.{ConsumerId}";

        public string Status
        {
            get
            {
                var pendingMessageCount = SlidingDoors.Sum(d => d.Value.MessageCount);
                var slidingDoors = SlidingDoors.Select(d => new { d.Key, d.Value.MessageCount }).ToArray();
                return new {pendingMessageCount, slidingDoors}.ToJson();
            }
        }
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

        protected abstract void PollMessages(CancellationToken cancellationToken);


        protected virtual void ReceiveMessages(CancellationTokenSource cancellationTokenSource)
        {
            #region peek messages that not been consumed since last time

            Logger.LogDebug($"{Id} ReceiveMessages start");
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    PollMessages(cancellationTokenSource.Token);
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
            var remainMessageCount = 0;
            while ((remainMessageCount = SlidingDoors.Sum(d => d.Value.MessageCount)) > ConsumerConfig.FullLoadThreshold)
            {
                Task.Delay(ConsumerConfig.WaitInterval).Wait();
                Logger.LogWarning($"{Id} is full load sleep {ConsumerConfig.WaitInterval} ms, remain message count:{remainMessageCount} threshold:{ConsumerConfig.FullLoadThreshold}");
            }
        }


        protected void AddMessageOffset(string topic, int partition, long offset)
        {
            var slidingDoor = SlidingDoors.GetOrAdd(SlidingDoor.GetSlidingDoorKey(topic, partition),
                                                    key => new SlidingDoor(CommitOffset,
                                                                           topic,
                                                                           partition,
                                                                           Configuration.Instance.GetCommitPerMessage()));
            slidingDoor.AddOffset(offset);
        }

        /// <summary>
        /// </summary>
        /// <param name="messageOffset"></param>
        protected virtual void FinishConsumingMessage(MessageOffset messageOffset)
        {
            var slidingDoor = SlidingDoors.TryGetValue(messageOffset.SlidingDoorKey);
            if (slidingDoor == null)
            {
                throw new Exception("partition slidingDoor not exists");
            }
            slidingDoor.RemoveOffset(messageOffset);
        }

        private void CommitOffset(MessageOffset messageOffset)
        {
            try
            {
                CommitOffsetAsync(messageOffset.Broker,
                                  messageOffset.Topic,
                                  messageOffset.Partition,
                                  messageOffset.Offset)
                    .ContinueWith(t =>
                    {
                        if (t.IsFaulted)
                        {
                            Logger.LogError(t.Exception, $"CommitOFfsetAsync failed");
                        }
                    });
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"CommitOffset failed {messageOffset.ToJson()}");
            }
        }

        public abstract Task CommitOffsetAsync(string broker, string topic, int partition, long offset);
    }
}