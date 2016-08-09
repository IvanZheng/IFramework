namespace IFramework.MessageQueue.MSKafka
{
    public class SubscriptionClient : KafkaConsumer
    {
        public SubscriptionClient(string topic, int partition, string subscription, string zkConnectionString)
            : base(zkConnectionString, topic, partition, $"{subscription}.{partition}")
        {

        }
    }
}
