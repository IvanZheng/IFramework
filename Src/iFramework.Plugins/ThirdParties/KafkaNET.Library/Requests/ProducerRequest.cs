using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Kafka.Client.Messages;
using Kafka.Client.Producers;
using Kafka.Client.Responses;
using Kafka.Client.Serialization;
using Kafka.Client.Utils;

namespace Kafka.Client.Requests
{
    /// <summary>
    ///     Constructs a request to send to Kafka.
    /// </summary>
    public class ProducerRequest : AbstractRequest, IWritable
    {
        public const int RandomPartition = -1;
        public const short CurrentVersion = 0;
        public const byte DefaultTopicSizeSize = 2;
        public const byte DefaultPartitionSize = 4;
        public const byte DefaultSetSizeSize = 4;

        public const byte DefaultHeaderSize = DefaultRequestSizeSize +
                                              DefaultTopicSizeSize +
                                              DefaultPartitionSize +
                                              DefaultRequestIdSize +
                                              DefaultSetSizeSize;

        public const byte VersionIdSize = 2;
        public const byte CorrelationIdSize = 4;
        public const byte ClientIdSize = 2;
        public const byte RequiredAcksSize = 2;
        public const byte AckTimeoutSize = 4;
        public const byte NumberOfTopicsSize = 4;

        public const byte DefaultHeaderSize8 = DefaultRequestSizeSize +
                                               DefaultRequestIdSize +
                                               VersionIdSize +
                                               CorrelationIdSize +
                                               RequiredAcksSize +
                                               AckTimeoutSize +
                                               NumberOfTopicsSize;

        public ProducerRequest(short versionId,
                               int correlationId,
                               string clientId,
                               short requiredAcks,
                               int ackTimeout,
                               IEnumerable<TopicData> data)
        {
            VersionId = versionId;
            CorrelationId = correlationId;
            ClientId = clientId;
            RequiredAcks = requiredAcks;
            AckTimeout = ackTimeout;
            Data = data;
            var length = GetRequestLength();
            RequestBuffer = new BoundedBuffer(length);
            WriteTo(RequestBuffer);
        }

        /// <summary>
        ///     This function should be obsolete since it's not sync with java version.
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="clientId"></param>
        /// <param name="requiredAcks"></param>
        /// <param name="ackTimeout"></param>
        /// <param name="data"></param>
        public ProducerRequest(int correlationId,
                               string clientId,
                               short requiredAcks,
                               int ackTimeout,
                               IEnumerable<TopicData> data)
            : this(CurrentVersion, correlationId, clientId, requiredAcks, ackTimeout, data) { }

        /// <summary>
        ///     Based on messageset per topic/partition, group it by topic and send out.
        ///     Sync with java/scala version , do group by topic inside this class.
        ///     https://git-wip-us.apache.org/repos/asf?p=kafka.git;a=blob;f=core/src/main/scala/kafka/api/ProducerRequest.scala;h=570b2da1d865086f9830aa919a49063abbbe574d;hb=HEAD
        ///     private lazy val dataGroupedByTopic = data.groupBy(_._1.topic)
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="clientId"></param>
        /// <param name="requiredAcks"></param>
        /// <param name="ackTimeout"></param>
        /// <param name="messagesPerTopic"></param>
        public ProducerRequest(int correlationId,
                               string clientId,
                               short requiredAcks,
                               int ackTimeout,
                               IDictionary<TopicAndPartition, BufferedMessageSet> messagesPerTopic)
        {
            var topicName = string.Empty;
            var partitionId = -1;
            var topics = new Dictionary<string, List<PartitionData>>();
            foreach (var keyValuePair in messagesPerTopic)
            {
                topicName = keyValuePair.Key.Topic;
                partitionId = keyValuePair.Key.PartitionId;
                var messagesSet = keyValuePair.Value;
                if (!topics.ContainsKey(topicName))
                {
                    topics.Add(topicName, new List<PartitionData>()); //create a new list for this topic
                }
                topics[topicName].Add(new PartitionData(partitionId, messagesSet));
            }

            VersionId = CurrentVersion;
            CorrelationId = correlationId;
            ClientId = clientId;
            RequiredAcks = requiredAcks;
            AckTimeout = ackTimeout;
            Data = topics.Select(kv => new TopicData(kv.Key, kv.Value));
            var length = GetRequestLength();
            RequestBuffer = new BoundedBuffer(length);
            WriteTo(RequestBuffer);
        }

        public short VersionId { get; set; }
        public int CorrelationId { get; set; }
        public string ClientId { get; set; }
        public short RequiredAcks { get; set; }
        public int AckTimeout { get; set; }
        public IEnumerable<TopicData> Data { get; set; }
        public BufferedMessageSet MessageSet { get; private set; }

        public override RequestTypes RequestType => RequestTypes.Produce;

        public int TotalSize => (int) RequestBuffer.Length;

        /// <summary>
        ///     Writes content into given stream
        /// </summary>
        /// <param name="output">
        ///     The output stream.
        /// </param>
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

        /// <summary>
        ///     Writes content into given writer
        /// </summary>
        /// <param name="writer">
        ///     The writer.
        /// </param>
        public void WriteTo(KafkaBinaryWriter writer)
        {
            Guard.NotNull(writer, "writer");

            writer.Write(VersionId);
            writer.Write(CorrelationId);
            writer.WriteShortString(ClientId);
            writer.Write(RequiredAcks);
            writer.Write(AckTimeout);
            writer.Write(Data.Count());
            foreach (var topicData in Data)
            {
                writer.WriteShortString(topicData.Topic);
                writer.Write(topicData.PartitionData.Count());
                foreach (var partitionData in topicData.PartitionData)
                {
                    writer.Write(partitionData.Partition);
                    writer.Write(partitionData.MessageSet.SetSize);
                    partitionData.MessageSet.WriteTo(writer);
                }
            }
        }

        public int GetRequestLength()
        {
            return DefaultHeaderSize8 +
                   GetShortStringWriteLength(ClientId) +
                   Data.Sum(item => item.SizeInBytes);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("Request size: ");
            sb.Append(TotalSize);
            sb.Append(", RequestId: ");
            sb.Append(RequestTypeId);
            sb.Append("(");
            sb.Append((RequestTypes) RequestTypeId);
            sb.Append(")");
            sb.Append(", ClientId: ");
            sb.Append(ClientId);
            sb.Append(", Version: ");
            sb.Append(VersionId);
            sb.Append(", Set size: ");
            sb.Append(MessageSet.SetSize);
            sb.Append(", Set {");
            sb.Append(MessageSet);
            sb.Append("}");
            return sb.ToString();
        }
    }
}