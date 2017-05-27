using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Kafka.Client.Responses;
using Kafka.Client.Serialization;
using Kafka.Client.Utils;

namespace Kafka.Client.Consumers
{
    public class OffsetResponse
    {
        public OffsetResponse(int correlationId, Dictionary<string, List<PartitionOffsetsResponse>> responseMap)
        {
            CorrelationId = correlationId;
            ResponseMap = responseMap;
        }

        public int CorrelationId { get; }
        public Dictionary<string, List<PartitionOffsetsResponse>> ResponseMap { get; }

        public override string ToString()
        {
            var sb = new StringBuilder(1024);
            sb.AppendFormat("OffsetResponse.CorrelationId:{0},ResponseMap Count={1}", CorrelationId, ResponseMap.Count);

            var i = 0;
            foreach (var v in ResponseMap)
            {
                sb.AppendFormat(",ResponseMap[{0}].Key:{1},PartitionOffsetsResponse Count={2}", i, v.Key,
                    v.Value.Count);
                var j = 0;
                foreach (var o in v.Value)
                {
                    sb.AppendFormat(",PartitionOffsetsResponse[{0}]:{1}", j, o);
                    j++;
                }
                i++;
            }

            var s = sb.ToString();
            sb.Length = 0;
            return s;
        }

        public class Parser : IResponseParser<OffsetResponse>
        {
            public OffsetResponse ParseFrom(KafkaBinaryReader reader)
            {
                reader.ReadInt32(); // skipping first int
                var correlationId = reader.ReadInt32();
                var numTopics = reader.ReadInt32();
                var responseMap = new Dictionary<string, List<PartitionOffsetsResponse>>();
                for (var i = 0; i < numTopics; ++i)
                {
                    var topic = reader.ReadShortString();
                    var numPartitions = reader.ReadInt32();
                    var responses = new List<PartitionOffsetsResponse>();
                    for (var p = 0; p < numPartitions; ++p)
                        responses.Add(PartitionOffsetsResponse.ReadFrom(reader));

                    responseMap[topic] = responses;
                }

                return new OffsetResponse(correlationId, responseMap);
            }
        }
    }

    public class PartitionOffsetsResponse
    {
        public PartitionOffsetsResponse(int partitionId, ErrorMapping error, List<long> offsets)
        {
            PartitionId = partitionId;
            Error = error;
            Offsets = offsets;
        }

        public int PartitionId { get; }
        public ErrorMapping Error { get; }
        public List<long> Offsets { get; }

        public override string ToString()
        {
            var sb = new StringBuilder(1024);

            sb.AppendFormat("PartitionOffsetsResponse.PartitionId:{0},Error:{1},Offsets Count={2}", PartitionId, Error,
                Offsets.Count);
            var i = 0;
            foreach (var o in Offsets)
            {
                sb.AppendFormat("Offsets[{0}]:{1}", i, o);
                i++;
            }

            var s = sb.ToString();
            sb.Length = 0;
            return s;
        }

        public static PartitionOffsetsResponse ReadFrom(KafkaBinaryReader reader)
        {
            var partitionId = reader.ReadInt32();
            var error = reader.ReadInt16();
            var numOffsets = reader.ReadInt32();
            var offsets = new List<long>();
            for (var o = 0; o < numOffsets; ++o)
                offsets.Add(reader.ReadInt64());

            return new PartitionOffsetsResponse(partitionId,
                (ErrorMapping) Enum.Parse(typeof(ErrorMapping), error.ToString(CultureInfo.InvariantCulture)),
                offsets);
        }
    }
}