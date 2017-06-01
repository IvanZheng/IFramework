using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Kafka.Client.Messages;
using Kafka.Client.Serialization;

namespace Kafka.Client.Consumers
{
    /// <summary>
    ///     This class is a thread-safe IEnumerable of <see cref="Message" /> that can be enumerated to get messages.
    /// </summary>
    public class KafkaMessageStream<TData> : IKafkaMessageStream<TData>
    {
        private readonly int consumerTimeoutMs;

        private readonly IDecoder<TData> decoder;
        private readonly BlockingCollection<FetchedDataChunk> queue;

        private readonly string topic;

        internal KafkaMessageStream(string topic,
                                    BlockingCollection<FetchedDataChunk> queue,
                                    int consumerTimeoutMs,
                                    IDecoder<TData> decoder)
        {
            this.topic = topic;
            this.consumerTimeoutMs = consumerTimeoutMs;
            this.queue = queue;
            this.decoder = decoder;
            iterator = new ConsumerIterator<TData>(this.topic, this.queue, this.consumerTimeoutMs, this.decoder);
        }

        internal KafkaMessageStream(string topic,
                                    BlockingCollection<FetchedDataChunk> queue,
                                    int consumerTimeoutMs,
                                    IDecoder<TData> decoder,
                                    CancellationToken token)
        {
            this.topic = topic;
            this.consumerTimeoutMs = consumerTimeoutMs;
            this.queue = queue;
            this.decoder = decoder;
            iterator = new ConsumerIterator<TData>(topic, queue, consumerTimeoutMs, decoder, token);
        }

        public IConsumerIterator<TData> iterator { get; }

        public IKafkaMessageStream<TData> GetCancellable(CancellationToken cancellationToken)
        {
            return new KafkaMessageStream<TData>(topic, queue, consumerTimeoutMs, decoder, cancellationToken);
        }

        public int Count => queue.Count;

        public void Clear()
        {
            iterator.ClearIterator();
        }

        public IEnumerator<TData> GetEnumerator()
        {
            return iterator;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}