using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using IFramework.Infrastructure.Logging;
using IFramework.IoC;
using Kafka.Client.Messages;

namespace Kafka.Client.Consumers
{
    /// <summary>
    ///     Represents topic in brokers's partition.
    /// </summary>
    public class PartitionTopicInfo
    {
        public static ILogger Logger = IoCFactory.Resolve<ILoggerFactory>().Create(typeof(PartitionTopicInfo));
        private readonly BlockingCollection<FetchedDataChunk> chunkQueue;
        private readonly object commitedOffsetLock = new object();
        private readonly object consumedOffsetLock = new object();
        private readonly object fetchedOffsetLock = new object();
        private long commitedOffset;
        private long consumedOffset;
        private long fetchedOffset;
        private long lastKnownGoodNextRequestOffset;
        private long nextRequestOffset;

        /// <summary>
        ///     Initializes a new instance of the <see cref="PartitionTopicInfo" /> class.
        /// </summary>
        /// <param name="topic">
        ///     The topic.
        /// </param>
        /// <param name="brokerId">
        ///     The broker ID.
        /// </param>
        /// <param name="partition">
        ///     The broker's partition.
        /// </param>
        /// <param name="chunkQueue">
        ///     The chunk queue.
        /// </param>
        /// <param name="consumedOffset">
        ///     The consumed offset value.
        /// </param>
        /// <param name="fetchedOffset">
        ///     The fetched offset value.
        /// </param>
        /// <param name="fetchSize">
        ///     The fetch size.
        /// </param>
        public PartitionTopicInfo(
            string topic,
            int brokerId,
            int partitionId,
            BlockingCollection<FetchedDataChunk> chunkQueue,
            long consumedOffset,
            long fetchedOffset,
            int fetchSize,
            long commitedOffset)
        {
            Topic = topic;
            PartitionId = partitionId;
            this.chunkQueue = chunkQueue;
            BrokerId = brokerId;
            this.consumedOffset = consumedOffset;
            this.fetchedOffset = fetchedOffset;
            NextRequestOffset = fetchedOffset;
            FetchSize = fetchSize;
            this.commitedOffset = commitedOffset;
            //if (Logger.IsDebugEnabled)
            {
                Logger.DebugFormat("initial CommitedOffset  of {0} is {1}", this, commitedOffset);
                Logger.DebugFormat("initial consumer offset of {0} is {1}", this, consumedOffset);
                Logger.DebugFormat("initial fetch offset of {0} is {1}", this, fetchedOffset);
            }
        }

        /// <summary>
        ///     Gets broker ID.
        /// </summary>
        public int BrokerId { get; }

        /// <summary>
        ///     Gets the fetch size.
        /// </summary>
        public int FetchSize { get; }

        /// <summary>
        ///     Gets the partition Id.
        /// </summary>
        public int PartitionId { get; }

        /// <summary>
        ///     Gets the topic.
        /// </summary>
        public string Topic { get; }

        /// <summary>
        ///     Last read out from Iterator
        /// </summary>
        public long ConsumeOffset
        {
            get
            {
                lock (consumedOffsetLock)
                {
                    return consumedOffset;
                }
            }
            set
            {
                lock (consumedOffsetLock)
                {
                    consumedOffset = value;
                }

                //if (Logger.IsDebugEnabled)
                {
                    Logger.DebugFormat("reset consume offset of {0} to {1}", this, value);
                }
            }
        }

        /// <summary>
        ///     Last commited to zookeeper
        /// </summary>
        public long CommitedOffset
        {
            get
            {
                lock (commitedOffsetLock)
                {
                    return commitedOffset;
                }
            }
            set
            {
                lock (consumedOffsetLock)
                {
                    commitedOffset = value;
                }

                //if (Logger.IsDebugEnabled)
                {
                    Logger.DebugFormat("resetcommitedOffset of {0} to {1}", this, value);
                }
            }
        }

        /// <summary>
        ///     Gets or sets a flag which indicates if the current consumed offset is valid
        ///     and should be used during updates to ZooKeeper.
        /// </summary>
        public bool ConsumeOffsetValid { get; set; } = true;

        /// <summary>
        ///     Last fetched offset from Kafka
        /// </summary>
        public long FetchOffset
        {
            get
            {
                lock (fetchedOffsetLock)
                {
                    return fetchedOffset;
                }
            }

            set
            {
                lock (fetchedOffsetLock)
                {
                    fetchedOffset = value;
                    // reset the next offset as well.
                    NextRequestOffset = value;
                }

                //if (Logger.IsDebugEnabled)
                {
                    Logger.DebugFormat("reset fetch offset of {0} to {1}", this, value);
                }
            }
        }

        // Gets or sets the offset for the next request.
        public long NextRequestOffset
        {
            get => nextRequestOffset;
            internal set
            {
                nextRequestOffset = value;

                // keep lastKnownGoodNextRequestOffset in sync with nextRequestOffset in normal case
                lastKnownGoodNextRequestOffset = value;
            }
        }

        /// <summary>
        ///     It happens when it gets consumer offset out of range exception. Try to fix the offset.
        /// </summary>
        internal void ResetOffset(long resetOffset)
        {
            lock (fetchedOffsetLock)
            {
                /// Save the old fetchoffset as lastKnownGoodNextRequestOffset in order to caculate how many actual messages are left in the queue
                /// Reset the fetchedoffset and nextrequestoffset to where it should be.
                lastKnownGoodNextRequestOffset = nextRequestOffset;
                fetchedOffset = resetOffset;
                nextRequestOffset = resetOffset;
            }

            //if (Logger.IsDebugEnabled)
            {
                Logger.DebugFormat("Set lastKnownGoodNextRequestOffset to {0}. {1}", lastKnownGoodNextRequestOffset,
                    this);
            }
        }

        internal long GetMessagesCount()
        {
            return lastKnownGoodNextRequestOffset - ConsumeOffset;
        }

        /// <summary>
        ///     Ads a message set to the queue
        /// </summary>
        /// <param name="messages">The message set</param>
        /// <returns>The set size</returns>
        public int Add(BufferedMessageSet messages)
        {
            var size = messages.SetSize;
            if (size > 0)
            {
                var offset = messages.Messages.Last().Offset;

                Logger.InfoFormat("{2} : Updating fetch offset = {0} with value = {1}", fetchedOffset, offset,
                    PartitionId);
                chunkQueue.Add(new FetchedDataChunk(messages, this, fetchedOffset));
                Interlocked.Exchange(ref fetchedOffset, offset);
                Logger.Debug("Updated fetch offset of " + this + " to " + offset);
            }

            return size;
        }

        public override string ToString()
        {
            return string.Format("{0}:{1}: fetched offset = {2}: consumed offset = {3}", Topic, PartitionId,
                fetchedOffset, consumedOffset);
        }
    }
}