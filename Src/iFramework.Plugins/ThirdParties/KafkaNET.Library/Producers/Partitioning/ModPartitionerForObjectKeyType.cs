namespace Kafka.Client.Producers.Partitioning
{
    public class ModPartitionerForObjectKeyType : IPartitioner<object>
    {
        public int Partition(object key, int numPartitions)
        {
            return key.GetHashCode() % numPartitions;
        }
    }
}