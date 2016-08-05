namespace IFramework.MessageQueue.MSKafka
{
    public class QueueConsumer : KafkaConsumer
    {
        public QueueConsumer(string zkConnectionString, string queue, int partition)
            : base(zkConnectionString, queue, partition, $"{queue}.{partition}")
        {

        }
    }
}
