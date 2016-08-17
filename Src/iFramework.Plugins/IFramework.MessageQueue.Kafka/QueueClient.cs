using IFramework.Infrastructure.Logging;
using IFramework.IoC;
using Kafka.Client.Cfg;
using Kafka.Client.Consumers;
using Kafka.Client.Producers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace IFramework.MessageQueue.MSKafka
{
    public class QueueClient
    {
        Producer<string, Kafka.Client.Messages.Message> _producer;
        string _queue;
        ZooKeeperConfiguration _zooKeeperConfiguration;
        ILogger _logger = IoCFactory.Resolve<ILoggerFactory>().Create(typeof(QueueClient).Name);
        public QueueClient(string queue, string zkConnectionString)
        {
            _queue = queue;
            _zooKeeperConfiguration = KafkaClient.GetZooKeeperConfiguration(zkConnectionString);
            ProducerConfiguration producerConfiguration = new ProducerConfiguration(new List<BrokerConfiguration>())
            {
                AckTimeout = 30000,
                RequiredAcks = -1,
                ZooKeeper = _zooKeeperConfiguration
            };
            _producer = new Producer(producerConfiguration);
        }

        public void Stop()
        {
            try
            {
                if (_producer != null)
                {
                    _producer.Dispose();
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"{_queue} producer dispose failed", ex);
            }
        }

        public void Send(ProducerData<string, Kafka.Client.Messages.Message> data)
        {
            _producer.Send(data);
        }
    }
}
