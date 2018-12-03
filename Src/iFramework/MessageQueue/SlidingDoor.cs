using System;
using System.Collections.Generic;
using System.Linq;
using IFramework.DependencyInjection;
using IFramework.Infrastructure;
using Microsoft.Extensions.Logging;

namespace IFramework.MessageQueue
{
    public class SlidingDoor : ISlidingDoor
    {
        private readonly object _removeOffsetLock = new object();
        protected Action<MessageOffset> CommitOffset;
        protected readonly string Topic;
        protected bool CommitPerMessage;
        protected long ConsumedOffset = -1L;
        protected long LastCommittedOffset = -1L;
        protected long LastOffset = -1L;
        protected SortedSet<long> Offsets;
        protected SortedList<long, MessageOffset> RemovedMessageOffsets;
        protected int Partition;
        protected ILogger _logger;
        public static string GetSlidingDoorKey(string topic, int partition)
        {
            return $"{topic}.{partition}";
        }
        public SlidingDoor(Action<MessageOffset> commitOffset,
                           string topic,
                           int partition,
                           bool commitPerMessage = false)
        {
            CommitOffset = commitOffset;
            Topic = topic;
            Partition = partition;
            Offsets = new SortedSet<long>();
            RemovedMessageOffsets = new SortedList<long, MessageOffset>();
            CommitPerMessage = commitPerMessage;
            _logger = ObjectProviderFactory.GetService<ILoggerFactory>()
                                           .CreateLogger(GetType());
        }

        public int MessageCount => Offsets.Count;

        public void AddOffset(long offset)
        {
            if (!CommitPerMessage)
            {
                lock (_removeOffsetLock)
                {
                    Offsets.Add(offset);
                    LastOffset = offset;
                }
            }
        }

        public void RemoveOffset(MessageOffset messageOffset)
        {
            _logger.LogDebug($"{messageOffset.Broker}.{messageOffset.Topic}.{messageOffset.Partition} remove offset {messageOffset.Offset} remain:{Offsets.Count}");
            if (CommitPerMessage)
            {
                CommitOffset(messageOffset);
            }
            else
            {
                lock (_removeOffsetLock)
                {
                    if (Offsets.Remove(messageOffset.Offset))
                    {
                        RemovedMessageOffsets.Add(messageOffset.Offset, messageOffset);
                        if (Offsets.Count > 0)
                        {
                            ConsumedOffset = Offsets.First() - 1;
                        }
                        else
                        {
                            ConsumedOffset = LastOffset;
                        }
                    }
                    if (ConsumedOffset > LastCommittedOffset)
                    {
                        if (RemovedMessageOffsets.TryRemoveBeforeKey(ConsumedOffset, out var currentMessageOffset))
                        {
                            CommitOffset(currentMessageOffset);
                        }
                        LastCommittedOffset = ConsumedOffset;
                    }
                }
            }
        }
    }
}