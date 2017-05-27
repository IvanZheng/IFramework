using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kafka.Client.Messages;
using Kafka.Client.Serialization;
using Kafka.Client.Utils;

namespace Kafka.Client.Requests
{
    /// <summary>
    ///     Constructs a request to send to Kafka to get the current offset for a given topic
    /// </summary>
    public class OffsetRequest : AbstractRequest, IWritable
    {
        public const string SmallestTime = "smallest";
        public const string LargestTime = "largest";
        public const byte DefaultTopicSizeSize = 2;
        public const byte DefaultPartitionSize = 4;
        public const byte DefaultTimeSize = 8;
        public const byte DefaultReplicaIdSize = 4;
        public const byte DefaultRequestInfoSize = 4;
        public const byte DefaultPartitionCountSize = 4;

        public const byte DefaultHeaderSize = DefaultRequestSizeSize + DefaultRequestIdSize +
                                              2 + // version
                                              4; // correlation            

        /// <summary>
        ///     The latest time constant.
        /// </summary>
        public static readonly long LatestTime = -1L;

        /// <summary>
        ///     The earliest time constant.
        /// </summary>
        public static readonly long EarliestTime = -2L;

        /// <summary>
        ///     Initializes a new instance of the OffsetRequest class.
        /// </summary>
        public OffsetRequest(Dictionary<string,
                List<PartitionOffsetRequestInfo>> requestInfo,
            short versionId = 0,
            int correlationId = 0,
            string clientId = "",
            int replicaId = -1)
        {
            VersionId = versionId;
            ClientId = clientId;
            CorrelationId = correlationId;
            ReplicaId = replicaId;
            RequestInfo = requestInfo;
            RequestBuffer = new BoundedBuffer(GetRequestLength());
            WriteTo(RequestBuffer);
        }

        public short VersionId { get; }
        public int ReplicaId { get; }
        public int CorrelationId { get; }
        public string ClientId { get; }
        public Dictionary<string, List<PartitionOffsetRequestInfo>> RequestInfo { get; }

        public override RequestTypes RequestType => RequestTypes.Offsets;

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
            writer.Write(RequestInfo.Count);
            foreach (var kv in RequestInfo)
            {
                writer.WriteShortString(kv.Key);
                writer.Write(kv.Value.Count);
                foreach (var info in kv.Value)
                    info.WriteTo(writer);
            }
        }

        public int GetRequestLength()
        {
            return DefaultHeaderSize +
                   GetShortStringWriteLength(ClientId) +
                   DefaultReplicaIdSize +
                   DefaultRequestInfoSize +
                   RequestInfo.Keys.Sum(k => GetShortStringWriteLength(k)) +
                   RequestInfo.Values.Sum(v => DefaultPartitionCountSize +
                                               v.Count * PartitionOffsetRequestInfo.SizeInBytes);
        }
    }

    public class PartitionOffsetRequestInfo : IWritable
    {
        public PartitionOffsetRequestInfo(int partitionId, long time, int maxNumOffsets)
        {
            PartitionId = partitionId;
            Time = time;
            MaxNumOffsets = maxNumOffsets;
        }

        public int PartitionId { get; }
        public long Time { get; }
        public int MaxNumOffsets { get; }

        public static int SizeInBytes => 4 + 8 + 4;

        public void WriteTo(MemoryStream output)
        {
            using (var writer = new KafkaBinaryWriter(output))
            {
                WriteTo(writer);
            }
        }

        public void WriteTo(KafkaBinaryWriter writer)
        {
            writer.Write(PartitionId);
            writer.Write(Time);
            writer.Write(MaxNumOffsets);
        }
    }
}