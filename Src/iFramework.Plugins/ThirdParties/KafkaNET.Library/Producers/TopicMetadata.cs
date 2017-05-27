using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Kafka.Client.Cluster;
using Kafka.Client.Requests;
using Kafka.Client.Serialization;
using Kafka.Client.Utils;

namespace Kafka.Client.Producers
{
    /// <summary>
    ///     TODO: Update summary.
    /// </summary>
    public class TopicMetadata : IWritable
    {
        public const byte DefaultNumOfPartitionsSize = 4;

        public TopicMetadata(string topic, IEnumerable<PartitionMetadata> partitionsMetadata, ErrorMapping error)
        {
            Topic = topic;
            PartitionsMetadata = partitionsMetadata;
            Error = error;
        }


        public string Topic { get; }
        public IEnumerable<PartitionMetadata> PartitionsMetadata { get; }
        public ErrorMapping Error { get; }

        public int SizeInBytes
        {
            get
            {
                var size = (int) BitWorks.GetShortStringLength(Topic, AbstractRequest.DefaultEncoding);
                foreach (var partitionMetadata in PartitionsMetadata)
                    size += DefaultNumOfPartitionsSize + partitionMetadata.SizeInBytes;
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

            writer.WriteShortString(Topic, AbstractRequest.DefaultEncoding);
            writer.Write(PartitionsMetadata.Count());
            foreach (var partitionMetadata in PartitionsMetadata)
                partitionMetadata.WriteTo(writer);
        }

        internal static TopicMetadata ParseFrom(KafkaBinaryReader reader, Dictionary<int, Broker> brokers)
        {
            var errorCode = reader.ReadInt16();
            var topic = BitWorks.ReadShortString(reader, AbstractRequest.DefaultEncoding);
            var numPartitions = reader.ReadInt32();
            var partitionsMetadata = new List<PartitionMetadata>();
            for (var i = 0; i < numPartitions; i++)
                partitionsMetadata.Add(PartitionMetadata.ParseFrom(reader, brokers));
            return new TopicMetadata(topic, partitionsMetadata, ErrorMapper.ToError(errorCode));
        }

        public override string ToString()
        {
            var sb = new StringBuilder(1024);
            sb.AppendFormat("TopicMetaData.Topic:{0},Error:{1},PartitionMetaData Count={2}", Topic, Error,
                PartitionsMetadata.Count());
            sb.AppendLine();

            var j = 0;
            foreach (var p in PartitionsMetadata)
            {
                sb.AppendFormat("PartitionMetaData[{0}]:{1}", j, p);
                j++;
            }

            var s = sb.ToString();
            sb.Length = 0;
            return s;
        }
    }
}