using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kafka.Client.Requests;
using Kafka.Client.Serialization;

namespace Kafka.Client.Producers
{
    /// <summary>
    ///     TODO: Update summary.
    /// </summary>
    public class TopicData
    {
        public const byte DefaultNumberOfPartitionsSize = 4;
        public const byte DefaultTopicSizeSize = 2;

        public TopicData(string topic, IEnumerable<PartitionData> partitionData)
        {
            Topic = topic;
            PartitionData = partitionData;
        }

        public string Topic { get; }

        public IEnumerable<PartitionData> PartitionData { get; }

        public int SizeInBytes
        {
            get
            {
                var topicLength = GetTopicLength(Topic);
                return DefaultTopicSizeSize + topicLength + DefaultNumberOfPartitionsSize +
                       PartitionData.Sum(dataPiece => dataPiece.SizeInBytes);
            }
        }

        internal static TopicData ParseFrom(KafkaBinaryReader reader)
        {
            var topic = reader.ReadShortString();
            var partitionCount = reader.ReadInt32();
            var partitions = new PartitionData[partitionCount];
            for (var i = 0; i < partitionCount; i++)
                partitions[i] = Producers.PartitionData.ParseFrom(reader);
            return new TopicData(topic, partitions.OrderBy(x => x.Partition));
        }

        internal static PartitionData FindPartition(IEnumerable<PartitionData> data, int partition)
        {
            if (data == null || !data.Any())
                return null;

            var low = 0;
            var high = data.Count() - 1;
            while (low <= high)
            {
                var mid = (low + high) / 2;
                var found = data.ElementAt(mid);
                if (found.Partition == partition)
                    return found;
                if (partition < found.Partition)
                    high = mid - 1;
                else
                    low = mid + 1;
            }
            return null;
        }

        protected static short GetTopicLength(string topic, string encoding = AbstractRequest.DefaultEncoding)
        {
            var encoder = Encoding.GetEncoding(encoding);
            return string.IsNullOrEmpty(topic)
                ? AbstractRequest.DefaultTopicLengthIfNonePresent
                : (short) encoder.GetByteCount(topic);
        }
    }
}