using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Kafka.Client.Cluster;
using Kafka.Client.Serialization;
using Kafka.Client.Utils;

namespace Kafka.Client.Producers
{
    /// <summary>
    ///     TODO: Update summary.
    /// </summary>
    public class PartitionMetadata : IWritable
    {
        public const int DefaultPartitionIdSize = 4;
        public const int DefaultIfLeaderExistsSize = 1;
        public const int DefaultNumberOfReplicasSize = 2;
        public const int DefaultNumberOfSyncReplicasSize = 2;
        public const int DefaultIfLogSegmentMetadataExistsSize = 1;

        public PartitionMetadata(int partitionId, Broker leader, IEnumerable<Broker> replicas, IEnumerable<Broker> isr)
        {
            PartitionId = partitionId;
            Leader = leader;
            Replicas = replicas;
            Isr = isr;
        }

        public int PartitionId { get; }
        public Broker Leader { get; }
        public IEnumerable<Broker> Replicas { get; }
        public IEnumerable<Broker> Isr { get; }

        public int SizeInBytes
        {
            get
            {
                var size = DefaultPartitionIdSize;
                if (Leader != null)
                {
                    size += Leader.SizeInBytes;
                }
                size += DefaultNumberOfReplicasSize;
                size += Replicas.Sum(replica => replica.SizeInBytes);
                size += DefaultNumberOfSyncReplicasSize;
                size += Isr.Sum(isr => isr.SizeInBytes);
                size += DefaultIfLogSegmentMetadataExistsSize;
                return size;
            }
        }

        public void WriteTo(MemoryStream output)
        {
            Guard.NotNull(output, "output");

            using (var writer = new KafkaBinaryWriter(output))
            {
                WriteTo(writer);
            }
        }

        public void WriteTo(KafkaBinaryWriter writer)
        {
            Guard.NotNull(writer, "writer");

            // if leader exists
            writer.Write(PartitionId);
            if (Leader != null)
            {
                writer.Write((byte) 1);
                Leader.WriteTo(writer);
            }
            else
            {
                writer.Write((byte) 0);
            }

            // number of replicas
            writer.Write((short) Replicas.Count());
            foreach (var replica in Replicas)
            {
                replica.WriteTo(writer);
            }

            // number of in-sync replicas
            writer.Write((short) Isr.Count());
            foreach (var isr in Isr)
            {
                isr.WriteTo(writer);
            }

            writer.Write((byte) 0);
        }

        public static PartitionMetadata ParseFrom(KafkaBinaryReader reader, Dictionary<int, Broker> brokers)
        {
            var errorCode = reader.ReadInt16();
            var partitionId = reader.ReadInt32();
            var leaderId = reader.ReadInt32();
            Broker leader = null;
            if (leaderId != -1)
            {
                leader = brokers[leaderId];
            }

            // list of all replicas
            var numReplicas = reader.ReadInt32();
            var replicas = new List<Broker>();
            for (var i = 0; i < numReplicas; ++i)
            {
                replicas.Add(brokers[reader.ReadInt32()]);
            }

            // list of in-sync replicas
            var numIsr = reader.ReadInt32();
            var isrs = new List<Broker>();
            for (var i = 0; i < numIsr; ++i)
            {
                isrs.Add(brokers[reader.ReadInt32()]);
            }

            return new PartitionMetadata(partitionId, leader, replicas, isrs);
        }

        public override string ToString()
        {
            var sb = new StringBuilder(4096);
            sb.AppendFormat(
                            "PartitionMetadata.ParitionId:{0},Leader:{1},Replicas Count:{2},Isr Count:{3}",
                            PartitionId,
                            Leader == null ? "null" : Leader.ToString(),
                            Replicas.Count(),
                            Isr.Count());

            var i = 0;
            foreach (var r in Replicas)
            {
                sb.AppendFormat(",Replicas[{0}]:{1}", i, r);
                i++;
            }

            i = 0;
            foreach (var sr in Isr)
            {
                sb.AppendFormat(",Isr[{0}]:{1}", i, sr);
                i++;
            }

            var s = sb.ToString();
            sb.Length = 0;
            return s;
        }
    }
}