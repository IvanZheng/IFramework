using IFramework.Infrastructure.Logging;
using IFramework.IoC;
using Kafka.Client.Cfg;
using Kafka.Client.Consumers;
using Kafka.Client.Helper;
using Kafka.Client.Producers;
using Kafka.Client.Requests;
using Kafka.Client.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IFramework.MessageQueue.MSKafka
{
    public class QueueClient
    {
        Producer<string, Kafka.Client.Messages.Message> _producer;
        string _queue;
        KafkaMessageStream<Kafka.Client.Messages.Message> _stream;
        ZooKeeperConfiguration _zooKeeperConfiguration;
        ZookeeperConsumerConnector _zkConsumerConnector;
        ILogger _logger = IoCFactory.Resolve<ILoggerFactory>().Create(typeof(QueueClient).Name);

        public QueueClient(string queue, string zkConnectionString)
        {
            _queue = queue;
            _zooKeeperConfiguration = new ZooKeeperConfiguration(zkConnectionString, 3000, 3000, 3000);
            ProducerConfiguration producerConfiguration = new ProducerConfiguration(new List<BrokerConfiguration>())
            {
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

            try
            {
                if (_zkConsumerConnector != null)
                {
                    _zkConsumerConnector.Dispose();
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"{_queue} zkConsumerConnector dispose failed", ex);
            }
        }

        public void Send(ProducerData<string, Kafka.Client.Messages.Message> data)
        {
            _producer.Send(data);
        }

        public IEnumerable<Kafka.Client.Messages.Message> PeekBatch(CancellationToken cancellationToken)
        {
            return Stream.GetCancellable(cancellationToken);
        }

        public void CommitOffset(long offset)
        {
            ZkConsumerConnector.CommitOffset(_queue, 0, offset, false);
        }

        ZookeeperConsumerConnector ZkConsumerConnector
        {
            get
            {
                if (_zkConsumerConnector == null)
                {
                    ConsumerConfiguration consumerConfiguration = new ConsumerConfiguration
                    {
                        AutoCommit = false,
                        GroupId = _queue,
                        ConsumerId = _queue,
                        MaxFetchBufferLength = KafkaSimpleManagerConfiguration.DefaultBufferSize,
                        FetchSize = KafkaSimpleManagerConfiguration.DefaultFetchSize,
                        AutoOffsetReset = OffsetRequest.LargestTime,
                        NumberOfTries = 3,
                        ZooKeeper = _zooKeeperConfiguration
                    };
                    _zkConsumerConnector = new ZookeeperConsumerConnector(consumerConfiguration, true);
                }
                return _zkConsumerConnector;
            }
        }

        KafkaMessageStream<Kafka.Client.Messages.Message> Stream
        {
            get
            {
                if (_stream == null)
                {
                    var topicCount = new Dictionary<string, int> {
                                    { _queue, 1}
                                 };
                    var streams = ZkConsumerConnector.CreateMessageStreams(topicCount, new DefaultDecoder());
                    _stream = streams[_queue][0];
                }
                return _stream;
            }
        }
    }
}
