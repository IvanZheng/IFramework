using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using ECommon.Socketing;
using IFramework.MessageQueue.Client.Abstracts;

namespace IFramework.MessageQueue.EQueue
{
    public class EQueueClientProvider : IMessageQueueClientProvider
    {
        private readonly string _clusterName;
        private readonly DefaultEQueueMessageContextBuilder _defaultMessageContextBuilder = new DefaultEQueueMessageContextBuilder();
        private readonly List<IPEndPoint> _nameServerList;

        public EQueueClientProvider(string clusterName, string nameServerList, int nameServerPort)
        {
            _clusterName = clusterName;
            _nameServerList = GetIpEndPoints(nameServerList, nameServerPort).ToList();
        }

        public IMessageConsumer CreateQueueConsumer(string commandQueueName,
                                                    OnMessagesReceived onMessagesReceived,
                                                    string consumerId,
                                                    ConsumerConfig consumerConfig,
                                                    bool start = true,
                                                    IMessageContextBuilder messageContextBuilder = null)
        {
            messageContextBuilder ??= _defaultMessageContextBuilder;
            var consumer = new EQueueConsumer(_clusterName,
                                              _nameServerList,
                                              new[] { commandQueueName },
                                              commandQueueName,
                                              consumerId,
                                              BuildOnEQueueMessageReceived(onMessagesReceived, messageContextBuilder as IEQueueMessageContextBuilder),
                                              consumerConfig);
            if (start)
            {
                consumer.Start();
            }

            return consumer;
        }


        public IMessageProducer CreateQueueProducer(string queue, ProducerConfig config = null)
        {
            return new EQueueProducer(_clusterName, _nameServerList, config).Start();
        }

        public IMessageProducer CreateTopicProducer(string topic, ProducerConfig config = null)
        {
            return new EQueueProducer(_clusterName, _nameServerList, config).Start();
        }


        public IMessageConsumer CreateTopicSubscription(string[] topics,
                                                        string subscriptionName,
                                                        OnMessagesReceived onMessagesReceived,
                                                        string consumerId,
                                                        ConsumerConfig consumerConfig,
                                                        bool start = true,
                                                        IMessageContextBuilder messageContextBuilder = null)
        {
            messageContextBuilder ??= _defaultMessageContextBuilder;
            var consumer = new EQueueConsumer(_clusterName,
                                              _nameServerList,
                                              topics,
                                              subscriptionName,
                                              consumerId,
                                              BuildOnEQueueMessageReceived(onMessagesReceived, messageContextBuilder as IEQueueMessageContextBuilder),
                                              consumerConfig);
            if (start)
            {
                consumer.Start();
            }

            return consumer;
        }

        public void Dispose() { }


        public static IEnumerable<IPEndPoint> GetIpEndPoints(string addresses, int defaultPort)
        {
            var nameServerIpEndPoints = new List<IPEndPoint>();
            if (string.IsNullOrEmpty(addresses))
            {
                nameServerIpEndPoints.Add(new IPEndPoint(SocketUtils.GetLocalIPV4(), defaultPort));
            }
            else
            {
                foreach (var address in addresses.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    try
                    {
                        var segments = address.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                        if (segments.Length == 2)
                        {
                            nameServerIpEndPoints.Add(new IPEndPoint(IPAddress.Parse(segments[0]),
                                                                     int.Parse(segments[1])
                                                                    )
                                                     );
                        }
                    }
                    catch (Exception) { }
                }
            }

            return nameServerIpEndPoints;
        }

        private OnEQueueMessageReceived BuildOnEQueueMessageReceived(OnMessagesReceived onMessagesReceived, IEQueueMessageContextBuilder messageContextBuilder)
        {
            return (consumer, message, cancellationToken) =>
            {
                var messageContext = messageContextBuilder.Build(message);
                onMessagesReceived(cancellationToken, messageContext);
            };
        }
    }
}