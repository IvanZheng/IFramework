namespace Kafka.Client.Cluster
{
    public class Replica
    {
        public Replica(int brokerId, string topic)
        {
            BrokerId = brokerId;
            Topic = topic;
        }

        public int BrokerId { get; }

        public string Topic { get; }
    }
}