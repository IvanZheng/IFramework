using Kafka.Client.Cfg;
using Kafka.Client.Producers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.MessageQueue.MSKafka
{
    public class TopicClient
    {
        Producer _producer;
        string _zkConnectionString;
        string _topic;
        public TopicClient(string topic, string zkConnectionString)
        {
            _topic = topic;
            _zkConnectionString = zkConnectionString;
            var producerConfiguration = new ProducerConfiguration(new List<BrokerConfiguration>())
            {
                RequiredAcks = -1,
                ZooKeeper = new ZooKeeperConfiguration(_zkConnectionString, 3000, 3000, 3000)
            };

            _producer = new Producer(producerConfiguration);
        }

        public void Send(ProducerData<string, Kafka.Client.Messages.Message> data)
        {
            _producer.Send(data);
        }

        public void Stop()
        {
            _producer?.Dispose();
        }
    }
}
