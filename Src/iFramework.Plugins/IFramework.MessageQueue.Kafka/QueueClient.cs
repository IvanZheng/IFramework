using Kafka.Client.Cfg;
using Kafka.Client.Consumers;
using Kafka.Client.Producers;
using Kafka.Client.Requests;
using Kafka.Client.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IFramework.MessageQueue.MSKafka
{
    public class QueueClient
    {
        ZookeeperConsumerConnector _zkConsumerConnector;
        Producer _producer;
        string _queue;
        KafkaMessageStream<Kafka.Client.Messages.Message> _stream;

        public QueueClient(string queue, string zkConnectionString)
        {
            _queue = queue;
            ConsumerConfiguration consumerConfiguration = new ConsumerConfiguration
            {
                AutoCommit = false,
                GroupId = queue,
                //ConsumerId = uniqueConsumerId,
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
                ZooKeeper = new ZooKeeperConfiguration(zkConnectionString, 3000, 3000, 3000)
            };
            _producer = new Producer(producerConfiguration);
        }

        public void Send(ProducerData<string, Kafka.Client.Messages.Message> data)
        {
            _producer.Send(data);
        }

        public IEnumerable<Kafka.Client.Messages.Message> PeekBatch(CancellationToken cancellationToken)
        {
            return _stream.GetCancellable(cancellationToken);
        }

        internal void CommitOffset(long offset)
        {
            _zkConsumerConnector.CommitOffset(_queue, 0, offset + 1);
        }
    }
}
