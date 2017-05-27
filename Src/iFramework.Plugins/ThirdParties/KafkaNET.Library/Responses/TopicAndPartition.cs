namespace Kafka.Client.Responses
{
    public class TopicAndPartition
    {
        public TopicAndPartition(string topic, int partitionId)
        {
            Topic = topic;
            PartitionId = partitionId;
        }

        public string Topic { get; }
        public int PartitionId { get; }

        public override int GetHashCode()
        {
            return Topic.GetHashCode() + 29 * PartitionId.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is TopicAndPartition)
            {
                var tp = (TopicAndPartition) obj;
                return Topic.Equals(tp.Topic) && PartitionId.Equals(tp.PartitionId);
            }

            return base.Equals(obj);
        }

        public override string ToString()
        {
            return string.Format("Topic:{0} PartitionID:{1}", Topic, PartitionId);
        }
    }
}