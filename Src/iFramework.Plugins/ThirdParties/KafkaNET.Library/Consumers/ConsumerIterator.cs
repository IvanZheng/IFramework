using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using IFramework.Infrastructure.Logging;
using IFramework.IoC;
using Kafka.Client.Exceptions;
using Kafka.Client.Messages;
using Kafka.Client.Serialization;

namespace Kafka.Client.Consumers
{
    /// <summary>
    ///     An iterator that blocks until a value can be read from the supplied queue.
    /// </summary>
    /// <remarks>
    ///     The iterator takes a shutdownCommand object which can be added to the queue to trigger a shutdown
    /// </remarks>
    public class ConsumerIterator<TData> : IConsumerIterator<TData>
    {
        public static ILogger Logger = IoCFactory.Resolve<ILoggerFactory>().Create(typeof(ConsumerIterator<TData>));

        private readonly CancellationToken cancellationToken;
        private readonly BlockingCollection<FetchedDataChunk> channel;
        private readonly int consumerTimeoutMs;
        private long consumedOffset = -1;
        private IEnumerator<MessageAndOffset> current;
        private FetchedDataChunk currentDataChunk;
        public volatile PartitionTopicInfo currentTopicInfo;
        private readonly IDecoder<TData> decoder;
        private readonly SemaphoreSlim makeNextSemaphore = new SemaphoreSlim(1, 1);
        private TData nextItem;
        private ConsumerIteratorState state = ConsumerIteratorState.NotReady;
        private string topic;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ConsumerIterator" /> class.
        /// </summary>
        /// <param name="channel">
        ///     The queue containing
        /// </param>
        /// <param name="consumerTimeoutMs">
        ///     The consumer timeout in ms.
        /// </param>
        public ConsumerIterator(string topic, BlockingCollection<FetchedDataChunk> channel, int consumerTimeoutMs,
            IDecoder<TData> decoder)
            : this(topic, channel, consumerTimeoutMs, decoder, CancellationToken.None)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ConsumerIterator" /> with a <see cref="CancellationToken" />
        /// </summary>
        /// <param name="channel">The queue containing</param>
        /// <param name="consumerTimeoutMs">The consumer timeout in ms</param>
        /// <param name="cacellationToken">The <see cref="CancellationToken" /> to allow for clean task cancellation</param>
        public ConsumerIterator(string topic, BlockingCollection<FetchedDataChunk> channel, int consumerTimeoutMs,
            IDecoder<TData> decoder, CancellationToken cancellationToken)
        {
            this.topic = topic;
            this.channel = channel;
            this.consumerTimeoutMs = consumerTimeoutMs;
            this.decoder = decoder;
            this.cancellationToken = cancellationToken;
        }

        /// <summary>
        ///     Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        /// <returns>
        ///     The element in the collection at the current position of the enumerator.
        /// </returns>
        public TData Current
        {
            get
            {
                if (!MoveNext())
                    throw new NoSuchElementException();

                state = ConsumerIteratorState.NotReady;
                if (nextItem != null)
                {
                    if (consumedOffset < 0)
                        throw new IllegalStateException(string.Format(CultureInfo.CurrentCulture,
                            "Byte offset returned by the message set is invalid {0}.", consumedOffset));

                    //if (consumedMessageOffset < 0)
                    //{
                    //    throw new IllegalStateException(String.Format(CultureInfo.CurrentCulture, "Ordinal offset returned by the message set is invalid {0}.", consumedMessageOffset));
                    //}

                    currentTopicInfo.ConsumeOffset = consumedOffset;
                    //currentTopicInfo.MessageOffset = consumedMessageOffset;
                    Logger.DebugFormat("Setting consumed offset to {0}", consumedOffset);

                    return nextItem;
                }

                throw new IllegalStateException("Expected item but none found.");
            }
        }

        /// <summary>
        ///     Gets the current element in the collection.
        /// </summary>
        /// <returns>
        ///     The current element in the collection.
        /// </returns>
        object IEnumerator.Current => Current;

        /// <summary>
        ///     Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        ///     true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of
        ///     the collection.
        /// </returns>
        public bool MoveNext()
        {
            if (state == ConsumerIteratorState.Failed)
                throw new IllegalStateException("Iterator is in failed state");

            switch (state)
            {
                case ConsumerIteratorState.Done:
                    return false;
                case ConsumerIteratorState.Ready:
                    return true;
                default:
                    return MaybeComputeNext();
            }
        }

        /// <summary>
        ///     Resets the enumerator's state to NotReady.
        /// </summary>
        public void Reset()
        {
            state = ConsumerIteratorState.NotReady;
        }

        public void Dispose()
        {
        }

        public void ClearIterator()
        {
            var semaphoreTaken = makeNextSemaphore.Wait(1000);
            try
            {
                while (channel.Count > 0)
                {
                    FetchedDataChunk item = null;
                    channel.TryTake(out item);
                }
                Logger.Info("Clearing the current data chunk for this consumer iterator");
                current = null;
            }
            finally
            {
                if (semaphoreTaken)
                    makeNextSemaphore.Release();
            }
        }

        private bool MaybeComputeNext()
        {
            state = ConsumerIteratorState.Failed;
            makeNextSemaphore.Wait();
            try
            {
                nextItem = MakeNext();
            }
            catch (OperationCanceledException)
            {
                state = ConsumerIteratorState.Done;
                return false;
            }
            finally
            {
                makeNextSemaphore.Release();
            }

            if (state == ConsumerIteratorState.Done)
                return false;

            state = ConsumerIteratorState.Ready;
            return true;
        }

        private TData MakeNext()
        {
            if (current == null || !current.MoveNext())
            {
                Logger.Debug("Getting new FetchedDataChunk...");
                if (consumerTimeoutMs < 0)
                {
                    currentDataChunk = channel.Take(cancellationToken);
                }
                else
                {
                    var done = channel.TryTake(out currentDataChunk, consumerTimeoutMs, cancellationToken);
                    if (!done)
                    {
                        Logger.Debug("Consumer iterator timing out...");
                        state = ConsumerIteratorState.NotReady;
                        throw new ConsumerTimeoutException();
                    }
                }

                if (currentDataChunk.Equals(ZookeeperConsumerConnector.ShutdownCommand))
                {
                    Logger.Debug("Received the shutdown command");
                    channel.Add(currentDataChunk);
                    return AllDone();
                }

                currentTopicInfo = currentDataChunk.TopicInfo;
                Logger.DebugFormat("CurrentTopicInfo: ConsumedOffset({0}), FetchOffset({1})",
                    currentTopicInfo.ConsumeOffset, currentTopicInfo.FetchOffset);
                if (currentTopicInfo.FetchOffset < currentDataChunk.FetchOffset)
                {
                    Logger.ErrorFormat(
                        "consumed offset: {0} doesn't match fetch offset: {1} for {2}; consumer may lose data",
                        currentTopicInfo.ConsumeOffset,
                        currentDataChunk.FetchOffset,
                        currentTopicInfo);
                    currentTopicInfo.ConsumeOffset = currentDataChunk.FetchOffset;
                }

                current = currentDataChunk.Messages.GetEnumerator();
                current.MoveNext();
            }

            var item = current.Current;
            consumedOffset = item.MessageOffset;

            return decoder.ToEvent(item.Message);
        }

        private TData AllDone()
        {
            state = ConsumerIteratorState.Done;
            return default(TData);
        }
    }
}