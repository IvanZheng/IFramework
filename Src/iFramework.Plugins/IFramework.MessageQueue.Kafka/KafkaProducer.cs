using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IFramework.Infrastructure.Logging;
using IFramework.IoC;
using Kafka.Client.Cfg;
using Kafka.Client.Producers;

namespace IFramework.MessageQueue.MSKafka
{
    public class KafkaProducer
    {
        private readonly ILogger _logger = IoCFactory.Resolve<ILoggerFactory>().Create(typeof(KafkaProducer).Name);
        private readonly Producer<string, Kafka.Client.Messages.Message> _producer;
        private readonly string _topic;
        private readonly ZooKeeperConfiguration _zooKeeperConfiguration;

        public KafkaProducer(string topic, string zkConnectionString)
        {
            _topic = topic;
            _zooKeeperConfiguration = KafkaClient.GetZooKeeperConfiguration(zkConnectionString);
            var producerConfiguration = new ProducerConfiguration(new List<BrokerConfiguration>())
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
                _logger.Error($"{_topic} producer dispose failed", ex);
            }
        }

        public async Task SendAsync(ProducerData<string, Kafka.Client.Messages.Message> data)
        {
            var retryTimes = 0;
            while (true)
            {
                retryTimes++;
                // 每次发送失败后线性增长等待发送时间 如: 5s, 10s, 15s, 20s .... max:5 minutes
                var waitTime = Math.Min(retryTimes * 1000 * 5, 60000 * 5);
                try
                {
                    _producer.Send(data);
                    break;
                }
                catch (Exception e)
                {
                    _logger.Error($"topic: {_topic} send message error", e);
                    await Task.Delay(waitTime);
                }
            }
        }
    }
}