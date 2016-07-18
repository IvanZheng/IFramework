using Kafka.Client.Cfg;
using Kafka.Client.Consumers;
using Kafka.Client.Producers;
using Kafka.Client.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.MessageQueue.MSKafka
{
    public class QueueClient
    {
        ZookeeperConsumerConnector _zkConsumerConnector;
        Producer _producer;
        string _queueName;

        public QueueClient(string queueName, string zkConnectionString)
        {
            ConsumerConfiguration consumerConfiguration = new ConsumerConfiguration
            {
                AutoCommit = false,
                GroupId = queueName,
                //ConsumerId = uniqueConsumerId,
                MaxFetchBufferLength = KafkaSimpleManagerConfiguration.DefaultBufferSize,
                FetchSize = KafkaSimpleManagerConfiguration.DefaultFetchSize,
                AutoOffsetReset = OffsetRequest.LargestTime,
                NumberOfTries = 3,
                ZooKeeper = new ZooKeeperConfiguration(zkConnectionString, 3000, 3000, 1000)
            };
            var zkConsumerConnector = new ZookeeperConsumerConnector(consumerConfiguration, true);

            ProducerConfiguration producerConfiguration = new ProducerConfiguration(new List<BrokerConfiguration>())
            {
                ZooKeeper = new ZooKeeperConfiguration(zkConnectionString, 3000, 3000, 3000)
            };
            _producer = new Producer(producerConfiguration);
        }
    }
}
