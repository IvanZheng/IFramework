using System;
using System.Collections.Generic;
using System.Linq;
using Kafka.Client.Cfg;
using Kafka.Client.Exceptions;
using Kafka.Client.Messages;
using Kafka.Client.Requests;
using Kafka.Client.Responses;
using Kafka.Client.Utils;

namespace Kafka.Client.Producers.Sync
{
    /// <summary>
    ///     Sends messages encapsulated in request to Kafka server synchronously
    /// </summary>
    public class SyncProducer : ISyncProducer
    {
        private readonly IKafkaConnection connection;
        private volatile bool disposed;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SyncProducer" /> class.
        /// </summary>
        /// <param name="config">
        ///     The producer config.
        /// </param>
        public SyncProducer(SyncProducerConfiguration config) : this(config, new KafkaConnection(
                                                                                                 config.Host,
                                                                                                 config.Port,
                                                                                                 config.BufferSize,
                                                                                                 config.SendTimeout,
                                                                                                 config.ReceiveTimeout,
                                                                                                 config.ReconnectInterval)) { }

        public SyncProducer(SyncProducerConfiguration config, IKafkaConnection connection)
        {
            Guard.NotNull(config, "config");
            Config = config;
            this.connection = connection;
        }

        /// <summary>
        ///     Gets producer config
        /// </summary>
        public SyncProducerConfiguration Config { get; }

        /// <summary>
        ///     Sends request to Kafka server synchronously
        /// </summary>
        /// <param name="request">
        ///     The request.
        /// </param>
        public ProducerResponse Send(ProducerRequest request)
        {
            EnsuresNotDisposed();

            foreach (var topicData in request.Data)
            foreach (var partitionData in topicData.PartitionData)
            {
                VerifyMessageSize(partitionData.MessageSet.Messages);
            }

            return connection.Send(request);
        }

        public IEnumerable<TopicMetadata> Send(TopicMetadataRequest request)
        {
            return connection.Send(request);
        }

        /// <summary>
        ///     Releases all unmanaged and managed resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            if (disposed)
            {
                return;
            }

            disposed = true;
            if (connection != null)
            {
                connection.Dispose();
            }
        }

        /// <summary>
        ///     Ensures that object was not disposed
        /// </summary>
        private void EnsuresNotDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        private void VerifyMessageSize(IEnumerable<Message> messages)
        {
            if (messages.Any(message => message.PayloadSize > Config.MaxMessageSize))
            {
                throw new MessageSizeTooLargeException();
            }
        }
    }
}