using Kafka.Client.Cfg;
using Kafka.Client.Consumers;
using Kafka.Client.Requests;
using Kafka.Client.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kafka.Client.Messages;
using System.Threading;

namespace IFramework.MessageQueue.MSKafka
{
    public class SubscriptionClient
    {
        string _topic;
        string _subscription;
        string _zkConnectionString;
        ZookeeperConsumerConnector _zkConsumerConnector;
        KafkaMessageStream<Kafka.Client.Messages.Message> _stream;

        public SubscriptionClient(string topic, string subscription, string zkConnectionString)
        {
            _topic = topic;
            _subscription = subscription;
            _zkConnectionString = zkConnectionString;
           
        }

        KafkaMessageStream<Kafka.Client.Messages.Message> Stream
        {
            get
            {
                if (_stream == null)
                {
                    ConsumerConfiguration consumerConfiguration = new ConsumerConfiguration
                    {
                        AutoCommit = false,
                        GroupId = _subscription,
                        ConsumerId = _subscription,
                        MaxFetchBufferLength = KafkaSimpleManagerConfiguration.DefaultBufferSize,
                        FetchSize = KafkaSimpleManagerConfiguration.DefaultFetchSize,
                        AutoOffsetReset = OffsetRequest.LargestTime,
                        NumberOfTries = 3,
                        ZooKeeper = new ZooKeeperConfiguration(_zkConnectionString, 3000, 3000, 1000)                      
                    };

                    _zkConsumerConnector = new ZookeeperConsumerConnector(consumerConfiguration, true);
                    // grab streams for desired topics 
                    var topicCount = new Dictionary<string, int>
                             {
                                { _topic, 1}
                             };
                    var streams = _zkConsumerConnector.CreateMessageStreams(topicCount, new DefaultDecoder());
                    _stream = streams[_topic][0];
                }
                return _stream;
            }
        }


        internal IEnumerable<Kafka.Client.Messages.Message> ReceiveMessages(CancellationToken token)
        {
            return Stream.GetCancellable(token);
        }

        internal void CommitOffset(long offset)
        {
            _zkConsumerConnector.CommitOffset(_topic, 0, offset, false);
        }

        internal void Stop()
        {
            if (_zkConsumerConnector != null)
            {
                _zkConsumerConnector.Dispose();
                _zkConsumerConnector = null;
                _stream = null;
            }
        }
    }
}
