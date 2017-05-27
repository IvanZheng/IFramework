using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Kafka.Client.Consumers;
using Kafka.Client.Messages;
using Kafka.Client.Serialization;
using Kafka.Client.Utils;

namespace Kafka.Client.Requests
{
    /// <summary>
    ///     Constructs a request to send to Kafka.
    ///     FetchRequest => ReplicaId MaxWaitTime MinBytes [TopicName [Partition FetchOffset MaxBytes]]
    ///     ReplicaId => int32
    ///     MaxWaitTime => int32
    ///     MinBytes => int32
    ///     TopicName => string
    ///     Partition => int32
    ///     FetchOffset => int64
    ///     MaxBytes => int32
    ///     set MaxWaitTime  to 0 and MinBytes to 0 can reduce latency.
    /// </summary>
    public class FetchRequest : AbstractRequest, IWritable
    {
        public const byte DefaultTopicSizeSize = 2;
        public const byte DefaultPartitionSize = 4;
        public const byte DefaultOffsetSize = 8;
        public const byte DefaultMaxSizeSize = 4;

        public const byte DefaultHeaderSize = DefaultRequestSizeSize + DefaultTopicSizeSize + DefaultPartitionSize +
                                              DefaultRequestIdSize + DefaultOffsetSize + DefaultMaxSizeSize;

        public const byte DefaultHeaderAsPartOfMultirequestSize =
            DefaultTopicSizeSize + DefaultPartitionSize + DefaultOffsetSize + DefaultMaxSizeSize;

        public const byte DefaultVersionIdSize = 2;
        public const byte DefaultCorrelationIdSize = 4;
        public const byte DefaultReplicaIdSize = 4;
        public const byte DefaultMaxWaitSize = 4;
        public const byte DefaultMinBytesSize = 4;
        public const byte DefaultOffsetInfoSizeSize = 4;

        public const short CurrentVersion = 0;

        public FetchRequest(int correlationId, string clientId, int maxWait, int minBytes,
            Dictionary<string, List<PartitionFetchInfo>> fetchInfos)
        {
            VersionId = CurrentVersion;
            CorrelationId = correlationId;
            ClientId = clientId;
            ReplicaId = -1;
            MaxWait = maxWait;
            MinBytes = minBytes;
            OffsetInfo = fetchInfos;
            var length = GetRequestLength();
            RequestBuffer = new BoundedBuffer(length);
            WriteTo(RequestBuffer);
        }

        public short VersionId { get; }
        public int CorrelationId { get; }
        public string ClientId { get; }
        public int ReplicaId { get; }
        public int MaxWait { get; }
        public int MinBytes { get; }
        public Dictionary<string, List<PartitionFetchInfo>> OffsetInfo { get; set; }

        public override RequestTypes RequestType => RequestTypes.Fetch;

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
            writer.Write(ReplicaId);
            writer.Write(MaxWait);
            writer.Write(MinBytes);
            writer.Write(OffsetInfo.Count);
            foreach (var offsetInfo in OffsetInfo)
            {
                writer.WriteShortString(offsetInfo.Key);
                writer.Write(offsetInfo.Value.Count);
                foreach (var v in offsetInfo.Value)
                    v.WriteTo(writer);
            }
        }

        public int GetRequestLength()
        {
            return DefaultRequestSizeSize +
                   DefaultRequestIdSize +
                   DefaultVersionIdSize +
                   DefaultCorrelationIdSize +
                   BitWorks.GetShortStringLength(ClientId, DefaultEncoding) +
                   DefaultReplicaIdSize +
                   DefaultMaxWaitSize +
                   DefaultMinBytesSize +
                   DefaultOffsetInfoSizeSize +
                   OffsetInfo.Keys.Sum(x => BitWorks.GetShortStringLength(x, DefaultEncoding)) + OffsetInfo.Values
                       .Select(pl => 4 + pl.Sum(p => p.SizeInBytes)).Sum();
        }

        public override string ToString()
        {
            return string.Format(
                CultureInfo.CurrentCulture,
                "varsionId: {0}, correlationId: {1}, clientId: {2}, replicaId: {3}, maxWait: {4}, minBytes: {5}, requestMap: {6}",
                VersionId,
                CorrelationId,
                ClientId,
                ReplicaId,
                MaxWait,
                MinBytes,
                string.Join(";",
                    OffsetInfo.Select(x => string.Format("[Topic:{0}, Info:{1}]", x.Key, string.Join("|", x.Value))))
            );
        }
    }
}