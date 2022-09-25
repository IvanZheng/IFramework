using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using ECommon.Socketing;
using IFramework.Infrastructure;
using IFramework.Message;
using IFramework.Message.Impl;
using IFramework.MessageQueue.Client.Abstracts;
using IFramework.MessageQueue.EQueue.MessageFormat;

namespace IFramework.MessageQueue.EQueue
{
    public class EQueueClientProvider : IMessageQueueClientProvider
    {
        private readonly string _clusterName;
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
                                                    bool start = true)
        {
            var consumer = new EQueueConsumer(_clusterName,
                                              _nameServerList,
                                              new[] {commandQueueName},
                                              commandQueueName,
                                              consumerId,
                                              BuildOnEQueueMessageReceived(onMessagesReceived),
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
                                                        bool start = true)
        {
            var consumer = new EQueueConsumer(_clusterName,
                                              _nameServerList,
                                              topics,
                                              subscriptionName,
                                              consumerId,
                                              BuildOnEQueueMessageReceived(onMessagesReceived),
                                              consumerConfig);
            if (start)
            {
                consumer.Start();
            }

            return consumer;
        }

        public void Dispose() { }


        public IMessageContext WrapMessage(string messageBody,
                                           string type,
                                           string correlationId = null,
                                           string topic = null,
                                           string key = null,
                                           string replyEndPoint = null,
                                           string messageId = null,
                                           SagaInfo sagaInfo = null,
                                           string producer = null)
        {
            var messageContext = new MessageContext(messageBody, type, messageId)
            {
                Producer = producer,
                Ip = Utility.GetLocalIpv4()?.ToString()
            };
            if (!string.IsNullOrEmpty(correlationId))
            {
                messageContext.CorrelationId = correlationId;
            }

            if (!string.IsNullOrEmpty(topic))
            {
                messageContext.Topic = topic;
            }

            if (!string.IsNullOrEmpty(key))
            {
                messageContext.Key = key;
            }

            if (!string.IsNullOrEmpty(replyEndPoint))
            {
                messageContext.ReplyToEndPoint = replyEndPoint;
            }

            if (sagaInfo != null)
            {
                messageContext.SagaInfo = sagaInfo;
            }

            return messageContext;
        }


        public IMessageContext WrapMessage(object message,
                                           string correlationId = null,
                                           string topic = null,
                                           string key = null,
                                           string replyEndPoint = null,
                                           string messageId = null,
                                           SagaInfo sagaInfo = null,
                                           string producer = null)
        {
            var messageContext = new MessageContext(message, messageId)
            {
                Producer = producer,
                Ip = Utility.GetLocalIpv4()?.ToString()
            };
            if (!string.IsNullOrEmpty(correlationId))
            {
                messageContext.CorrelationId = correlationId;
            }

            if (!string.IsNullOrEmpty(topic))
            {
                messageContext.Topic = topic;
            }

            if (!string.IsNullOrEmpty(key))
            {
                messageContext.Key = key;
            }

            if (!string.IsNullOrEmpty(replyEndPoint))
            {
                messageContext.ReplyToEndPoint = replyEndPoint;
            }

            if (sagaInfo != null)
            {
                messageContext.SagaInfo = sagaInfo;
            }

            return messageContext;
        }

        public static IEnumerable<IPEndPoint> GetIpEndPoints(string addresses, int defaultPort)
        {
            var nameServerIpEndPoints = new List<IPEndPoint>();
            if (string.IsNullOrEmpty(addresses))
            {
                nameServerIpEndPoints.Add(new IPEndPoint(SocketUtils.GetLocalIPV4(), defaultPort));
            }
            else
            {
                foreach (var address in addresses.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries))
                {
                    try
                    {
                        var segments = address.Split(new[] {':'}, StringSplitOptions.RemoveEmptyEntries);
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

        private OnEQueueMessageReceived BuildOnEQueueMessageReceived(OnMessagesReceived onMessagesReceived)
        {
            return (consumer, message, cancellationToken) =>
            {
                var equeueMessage = Encoding.UTF8
                                            .GetString(message.Body)
                                            .ToJsonObject<EQueueMessage>();
                var messageContext = new MessageContext(equeueMessage,
                                                        new MessageOffset(message.BrokerName, message.Topic, message.QueueId, message.QueueOffset));
                onMessagesReceived(cancellationToken, messageContext);
            };
        }
    }
}