using System;
using System.Collections.Generic;
using IFramework.Infrastructure.Logging;
using IFramework.IoC;
using Kafka.Client.Cfg;
using Kafka.Client.Exceptions;
using Kafka.Client.Producers;
using Kafka.Client.Requests;
using Kafka.Client.Responses;
using Kafka.Client.Utils;

namespace Kafka.Client.Consumers
{
    /// <summary>
    ///     The low-level API of consumer of Kafka messages
    /// </summary>
    /// <remarks>
    ///     Maintains a connection to a single broker and has a close correspondence
    ///     to the network requests sent to the server.
    ///     Also, is completely stateless.
    /// </remarks>
    public class Consumer : IConsumer
    {
        public static ILogger Logger = IoCFactory.Resolve<ILoggerFactory>().Create(typeof(Consumer));

        private readonly string host;
        private readonly int port;

        private KafkaConnection connection;

        internal long CreatedTimeInUTC;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Consumer" /> class.
        /// </summary>
        /// <param name="config">
        ///     The consumer configuration.
        /// </param>
        public Consumer(ConsumerConfiguration config)
        {
            Guard.NotNull(config, "config");

            Config = config;
            host = config.Broker.Host;
            port = config.Broker.Port;
            connection = new KafkaConnection(
                host,
                port,
                Config.BufferSize,
                Config.SendTimeout,
                Config.ReceiveTimeout,
                Config.ReconnectInterval);
            CreatedTimeInUTC = DateTime.UtcNow.Ticks;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Consumer" /> class.
        /// </summary>
        /// <param name="config">
        ///     The consumer configuration.
        /// </param>
        /// <param name="host"></param>
        /// <param name="port"></param>
        public Consumer(ConsumerConfiguration config, string host, int port)
        {
            Guard.NotNull(config, "config");

            Config = config;
            this.host = host;
            this.port = port;
            connection = new KafkaConnection(
                this.host,
                this.port,
                Config.BufferSize,
                Config.SendTimeout,
                Config.ReceiveTimeout,
                Config.ReconnectInterval);
        }

        public ConsumerConfiguration Config { get; }

        /// <summary>
        ///     Gets a list of valid offsets (up to maxSize) before the given time.
        /// </summary>
        /// <param name="request">
        ///     The offset request.
        /// </param>
        /// <returns>
        ///     The list of offsets, in descending order.
        /// </returns>
        public OffsetResponse GetOffsetsBefore(OffsetRequest request)
        {
            short tryCounter = 1;
            while (tryCounter <= Config.NumberOfTries)
                try
                {
                    lock (this)
                    {
                        return connection.Send(request);
                    }
                }
                catch (Exception ex)
                {
                    //// if maximum number of tries reached
                    if (tryCounter == Config.NumberOfTries)
                        throw;

                    tryCounter++;
                    Logger.InfoFormat("GetOffsetsBefore reconnect due to {0}", ex.FormatException());
                }

            return null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public IEnumerable<TopicMetadata> GetMetaData(TopicMetadataRequest request)
        {
            short tryCounter = 1;
            while (tryCounter <= Config.NumberOfTries)
                try
                {
                    lock (this)
                    {
                        return connection.Send(request);
                    }
                }
                catch (Exception ex)
                {
                    //// if maximum number of tries reached
                    if (tryCounter == Config.NumberOfTries)
                        throw;

                    tryCounter++;
                    Logger.InfoFormat("GetMetaData reconnect due to {0}", ex.FormatException());
                }

            return null;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                if (connection != null)
                    lock (this)
                    {
                        if (connection != null)
                        {
                            connection.Dispose();
                            connection = null;
                        }
                    }
        }

        #region Fetch

        public FetchResponse Fetch(string clientId, string topic, int correlationId, int partitionId, long fetchOffset,
            int fetchSize
            , int maxWaitTime, int minWaitSize)
        {
            var requestMap = new Dictionary<string, List<PartitionFetchInfo>>();
            requestMap.Add(
                topic,
                new List<PartitionFetchInfo>
                {
                    new PartitionFetchInfo(
                        partitionId,
                        fetchOffset,
                        fetchSize)
                });
            return Fetch(new FetchRequest(
                correlationId,
                clientId,
                maxWaitTime,
                minWaitSize,
                requestMap));
        }

        public FetchResponse Fetch(FetchRequest request)
        {
            short tryCounter = 1;
            while (tryCounter <= Config.NumberOfTries)
                try
                {
                    Logger.Debug("Fetch is waiting for send lock");
                    lock (this)
                    {
                        Logger.Debug("Fetch acquired send lock. Begin send");
                        return connection.Send(request);
                    }
                }
                catch (Exception ex)
                {
                    //// if maximum number of tries reached
                    if (tryCounter == Config.NumberOfTries)
                        throw;

                    tryCounter++;
                    Logger.InfoFormat("Fetch reconnect due to {0}", ex.FormatException());
                }

            return null;
        }

        /// <summary>
        ///     MANIFOLD use
        /// </summary>
        public FetchResponseWrapper FetchAndGetDetail(string clientId, string topic, int correlationId, int partitionId,
            long fetchOffset, int fetchSize
            , int maxWaitTime, int minWaitSize)
        {
            var response = Fetch(clientId,
                topic,
                correlationId,
                partitionId,
                fetchOffset,
                fetchSize,
                maxWaitTime,
                minWaitSize);
            if (response == null)
                throw new KafkaConsumeException(
                    string.Format(
                        "FetchRequest returned null FetchResponse,fetchOffset={0},leader={1},topic={2},partition={3}",
                        fetchOffset, Config.Broker, topic, partitionId));
            var partitionData = response.PartitionData(topic, partitionId);
            if (partitionData == null)
                throw new KafkaConsumeException(
                    string.Format(
                        "PartitionData int FetchResponse is null,fetchOffset={0},leader={1},topic={2},partition={3}",
                        fetchOffset, Config.Broker, topic, partitionId));
            if (partitionData.Error != ErrorMapping.NoError)
            {
                var s = string.Format(
                    "Partition data in FetchResponse has error. {0}  {1} fetchOffset={2},leader={3},topic={4},partition={5}"
                    , partitionData.Error, KafkaException.GetMessage(partitionData.Error)
                    , fetchOffset, Config.Broker, topic, partitionId);
                Logger.Error(s);
                throw new KafkaConsumeException(s, partitionData.Error);
            }
            return new FetchResponseWrapper(partitionData.GetMessageAndOffsets(), response.Size, response.CorrelationId,
                topic, partitionId);
        }

        #endregion
    }
}