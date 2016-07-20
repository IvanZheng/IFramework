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
        ZookeeperConsumerConnector _zkConsumerConnector;
        Producer<string, Kafka.Client.Messages.Message> _producer;
        string _queue;
        KafkaMessageStream<Kafka.Client.Messages.Message> _stream;
        ILogger _logger = IoCFactory.Resolve<ILoggerFactory>().Create(typeof(QueueClient).Name);

        public QueueClient(string queue, string zkConnectionString)
        {
            _queue = queue;
            ConsumerConfiguration consumerConfiguration = new ConsumerConfiguration
            {
                AutoCommit = false,
                GroupId = queue,
                ConsumerId = queue,
                MaxFetchBufferLength = KafkaSimpleManagerConfiguration.DefaultBufferSize,
                FetchSize = KafkaSimpleManagerConfiguration.DefaultFetchSize,
                AutoOffsetReset = OffsetRequest.LargestTime,
                NumberOfTries = 3,
                ZooKeeper = new ZooKeeperConfiguration(zkConnectionString, 3000, 3000, 1000)
            };
            _zkConsumerConnector = new ZookeeperConsumerConnector(consumerConfiguration, true);
            var topicCount = new Dictionary<string, int> {
                                    { _queue, 1}
                                 };
            var streams = _zkConsumerConnector.CreateMessageStreams(topicCount, new DefaultDecoder());
            _stream = streams[_queue][0];

            ProducerConfiguration producerConfiguration = new ProducerConfiguration(new List<BrokerConfiguration>())
            {
                RequiredAcks = 1,
                ZooKeeper = new ZooKeeperConfiguration(zkConnectionString, 3000, 3000, 3000)
            };
            _producer = new Producer(producerConfiguration);
        }

        public void Send(ProducerData<string, Kafka.Client.Messages.Message> data)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            _producer.Send(data);
            _logger.Debug($"{_queue} send data cost: {watch.Elapsed.TotalMilliseconds} ");
        }

        public IEnumerable<Kafka.Client.Messages.Message> PeekBatch(CancellationToken cancellationToken)
        {
            return _stream.GetCancellable(cancellationToken);
        }

        public void CommitOffset(long offset)
        {
            _zkConsumerConnector.CommitOffset(_queue, 0, offset, false);
        }
    }
}
