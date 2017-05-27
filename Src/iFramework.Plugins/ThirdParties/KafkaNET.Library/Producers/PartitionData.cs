using System.Collections.Generic;
using Kafka.Client.Messages;
using Kafka.Client.Serialization;
using Kafka.Client.Utils;

namespace Kafka.Client.Producers
{
    /// <summary>
    ///     PartitionData, contains buffered messageset
    /// </summary>
    public class PartitionData
    {
        public const byte DefaultPartitionIdSize = 4;
        public const byte DefaultMessagesSizeSize = 4;

        public PartitionData(int partition, ErrorMapping error, BufferedMessageSet messages)
        {
            Partition = partition;
            MessageSet = messages;
            Error = error;
            HighWaterMark = messages.HighwaterOffset;
        }

        public PartitionData(int partition, BufferedMessageSet messages)
            : this(partition, (short) ErrorMapping.NoError, messages)
        {
        }

        public int Partition { get; }
        public BufferedMessageSet MessageSet { get; }
        public long HighWaterMark { get; }
        public ErrorMapping Error { get; }

        public int SizeInBytes => DefaultPartitionIdSize + DefaultMessagesSizeSize + MessageSet.SetSize;

        internal static PartitionData ParseFrom(KafkaBinaryReader reader)
        {
            var partition = reader.ReadInt32();
            var error = reader.ReadInt16();
            var highWatermark = reader.ReadInt64();
            var messageSetSize = reader.ReadInt32();
            var bufferedMessageSet = BufferedMessageSet.ParseFrom(reader, messageSetSize, partition);
            return new PartitionData(partition, ErrorMapper.ToError(error), bufferedMessageSet);
        }

        public List<MessageAndOffset> GetMessageAndOffsets()
        {
            var listMessageAndOffsets = new List<MessageAndOffset>();
            //Seemly the MessageSet can only do traverse for one time.
            foreach (var m in MessageSet)
                listMessageAndOffsets.Add(m);
            return listMessageAndOffsets;
        }
    }
}