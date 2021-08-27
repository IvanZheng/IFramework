using Confluent.Kafka;

namespace IFramework.MessageQueue.ConfluentKafka
{
    public class ConsumerOffset
    {
        public string Topic { get; set; }
        public int Partition { get; set; }
        public long Offset { get; set; }
        public long LowOffset { get; set; }
        public long HighOffset { get; set; }

        public long Lag => Offset >= 0 ? HighOffset - Offset : HighOffset - LowOffset;

        public ConsumerOffset()
        {

        }

        public ConsumerOffset(string topic, int partition, long offset, WatermarkOffsets watermarkOffsets)
        {
            Topic = topic;
            Partition = partition;
            Offset = offset;
            LowOffset = watermarkOffsets.Low.Value;
            HighOffset = watermarkOffsets.High.Value;
        }
    }
}
