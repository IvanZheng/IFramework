using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kafka.Client.Cluster;
using Kafka.Client.Messages;
using Kafka.Client.Producers;
using Kafka.Client.Responses;
using Kafka.Client.Serialization;
using Kafka.Client.Utils;

namespace Kafka.Client.Requests
{
    /// <summary>
    ///     Kafka request to get topic metadata.
    /// </summary>
    public class TopicMetadataRequest : AbstractRequest, IWritable
    {
        private const int DefaultNumberOfTopicsSize = 4;
        private const byte DefaultHeaderSize8 = DefaultRequestSizeSize + DefaultRequestIdSize;
        private readonly string clientId;
        private readonly int correlationId;

        private readonly short versionId;

        private TopicMetadataRequest(IEnumerable<string> topics, short versionId, int correlationId, string clientId)
        {
            if (topics == null)
            {
                throw new ArgumentNullException("topics", "List of topics cannot be null.");
            }

            if (!topics.Any())
            {
                throw new ArgumentException("List of topics cannot be empty.");
            }

            Topics = new List<string>(topics);
            this.versionId = versionId;
            this.correlationId = correlationId;
            this.clientId = clientId;
            var length = GetRequestLength();
            RequestBuffer = new BoundedBuffer(length);
            WriteTo(RequestBuffer);
        }

        public IEnumerable<string> Topics { get; }
        public DetailedMetadataRequest DetailedMetadata { get; private set; }

        public override RequestTypes RequestType => RequestTypes.TopicMetadataRequest;

        public void WriteTo(MemoryStream output)
        {
            Guard.NotNull(output, "output");

            using (var writer = new KafkaBinaryWriter(output))
            {
                writer.Write(RequestBuffer.Capacity - DefaultRequestSizeSize);
                writer.Write(RequestTypeId);
                WriteTo(writer);
            }
        }

        public void WriteTo(KafkaBinaryWriter writer)
        {
            Guard.NotNull(writer, "writer");
            writer.Write(versionId);
            writer.Write(correlationId);
            writer.WriteShortString(clientId, DefaultEncoding);
            writer.Write(Topics.Count());
            foreach (var topic in Topics)
            {
                writer.WriteShortString(topic, DefaultEncoding);
            }
        }

        /// <summary>
        ///     Creates simple request with no segment metadata information
        /// </summary>
        /// <param name="topics">list of topics</param>
        /// <param name="versionId"></param>
        /// <param name="correlationId"></param>
        /// <param name="clientId"></param>
        /// <returns>request</returns>
        public static TopicMetadataRequest Create(IEnumerable<string> topics,
                                                  short versionId,
                                                  int correlationId,
                                                  string clientId)
        {
            return new TopicMetadataRequest(topics, versionId, correlationId, clientId);
        }

        public int GetRequestLength()
        {
            var size = DefaultHeaderSize8 +
                       FetchRequest.DefaultVersionIdSize +
                       FetchRequest.DefaultCorrelationIdSize +
                       BitWorks.GetShortStringLength(clientId, DefaultEncoding) +
                       DefaultNumberOfTopicsSize +
                       Topics.Sum(x => BitWorks.GetShortStringLength(x, DefaultEncoding));

            return size;
        }

        /// <summary>
        ///     https://cwiki.apache.org/confluence/display/KAFKA/A+Guide+To+The+Kafka+Protocol
        ///     MetadataResponse => [Broker][TopicMetadata]
        ///     Broker => NodeId Host Port
        ///     NodeId => int32
        ///     Host => string
        ///     Port => int32
        ///     TopicMetadata => TopicErrorCode TopicName [PartitionMetadata]
        ///     TopicErrorCode => int16
        ///     PartitionMetadata => PartitionErrorCode PartitionId Leader Replicas Isr
        ///     PartitionErrorCode => int16
        ///     PartitionId => int32
        ///     Leader => int32
        ///     Replicas => [int32]
        ///     Isr => [int32]
        /// </summary>
        public class Parser : IResponseParser<IEnumerable<TopicMetadata>>
        {
            public IEnumerable<TopicMetadata> ParseFrom(KafkaBinaryReader reader)
            {
                reader.ReadInt32();
                var correlationId = reader.ReadInt32();
                var brokerCount = reader.ReadInt32();
                var brokerMap = new Dictionary<int, Broker>();
                for (var i = 0; i < brokerCount; ++i)
                {
                    var broker = Broker.ParseFrom(reader);
                    brokerMap[broker.Id] = broker;
                }

                var numTopics = reader.ReadInt32();
                var topicMetadata = new TopicMetadata[numTopics];
                for (var i = 0; i < numTopics; i++)
                {
                    topicMetadata[i] = TopicMetadata.ParseFrom(reader, brokerMap);
                }
                return topicMetadata;
            }
        }
    }

    public enum DetailedMetadataRequest : short
    {
        SegmentMetadata = (short) 1,
        NoSegmentMetadata = 0
    }
}