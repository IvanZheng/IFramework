using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IFramework.Config;
using IFramework.Exceptions;
using IFramework.Infrastructure;
using IFramework.Message;
using IFramework.Message.Impl;

namespace IFramework.MessageQueue.InMemory
{
    public class InMemoryClient : IMessageQueueClient
    {
        private static readonly ConcurrentDictionary<string, List<SubscriptionClient>> SubscriptionClients = new ConcurrentDictionary<string, List<SubscriptionClient>>();
        private static readonly ConcurrentDictionary<string, BlockingCollection<IMessageContext>> CommandQueues = new ConcurrentDictionary<string, BlockingCollection<IMessageContext>>();

        public void Dispose() { }

        public Task SendAsync(IMessageContext messageContext, string queueTopic, CancellationToken cancellationToken)
        {
            queueTopic = Configuration.Instance.FormatMessageQueueName(queueTopic);
            var queue = CommandQueues.GetOrAdd(queueTopic, key => new BlockingCollection<IMessageContext>());
            queue.Add(messageContext, cancellationToken);
            return Task.FromResult<object>(null);
        }

        public Task PublishAsync(IMessageContext messageContext, string topic, CancellationToken cancellationToken)
        {
            topic = Configuration.Instance.FormatMessageQueueName(topic);
            var clients = SubscriptionClients.GetOrAdd(topic, key => new List<SubscriptionClient>());
            clients.ForEach(client => client.Enqueue(messageContext, cancellationToken));
            return Task.FromResult<object>(null);
        }

        public IMessageContext WrapMessage(object message, string correlationId = null, string topic = null, string key = null, string replyEndPoint = null, string messageId = null, SagaInfo sagaInfo = null, string producer = null)
        {
            if (message is Exception ex)
            {
                if (ex is DomainException domainException)
                {
                    // Remove inner Exception because it too large after serializing
                    message = new DomainException(domainException.ErrorCode, domainException.Message);
                }
                else
                {
                    message = new Exception(ex.GetBaseException().Message);
                }
            }
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
            if (string.IsNullOrWhiteSpace(messageContext.Key))
            {
                messageContext.Key = messageContext.MessageId;
            }
            if (!string.IsNullOrEmpty(replyEndPoint))
            {
                messageContext.ReplyToEndPoint = replyEndPoint;
            }

            if (!string.IsNullOrWhiteSpace(sagaInfo?.SagaId))
            {
                messageContext.SagaInfo = sagaInfo;
            }

            return messageContext;
        }

        public IMessageContext WrapMessage(string messageBody, string type, string correlationId = null, string topic = null, string key = null, string replyEndPoint = null, string messageId = null, SagaInfo sagaInfo = null, string producer = null)
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
            if (string.IsNullOrWhiteSpace(messageContext.Key))
            {
                messageContext.Key = messageContext.MessageId;
            }
            if (!string.IsNullOrEmpty(replyEndPoint))
            {
                messageContext.ReplyToEndPoint = replyEndPoint;
            }

            if (!string.IsNullOrWhiteSpace(sagaInfo?.SagaId))
            {
                messageContext.SagaInfo = sagaInfo;
            }

            return messageContext;
        }

        public IMessageConsumer StartSubscriptionClient(string topic,
                                                        string subscriptionName,
                                                        string consumerId,
                                                        OnMessagesReceived onMessagesReceived,
                                                        ConsumerConfig consumerConfig = null)
        {
            return StartSubscriptionClient(new[] {topic},
                                           subscriptionName,
                                           consumerId,
                                           onMessagesReceived,
                                           consumerConfig);
        }

        public IMessageConsumer StartSubscriptionClient(string[] topics,
                                                        string subscriptionName,
                                                        string consumerId,
                                                        OnMessagesReceived onMessagesReceived,
                                                        ConsumerConfig consumerConfig = null)
        {
            topics = topics.Select(topic => Configuration.Instance.FormatMessageQueueName(topic))
                           .ToArray();
            var client = new SubscriptionClient(topics, subscriptionName, consumerId, onMessagesReceived);

            topics.ForEach(topic =>
            {
                var clients = SubscriptionClients.GetOrAdd(topic, key => new List<SubscriptionClient>());
                clients.Add(client);
            });
            return client;
        }

        public IMessageConsumer StartQueueClient(string commandQueueName,
                                                 string consumerId,
                                                 OnMessagesReceived onMessagesReceived,
                                                 ConsumerConfig consumerConfig = null)
        {
            commandQueueName = Configuration.Instance.FormatMessageQueueName(commandQueueName);
            var queue = CommandQueues.GetOrAdd(commandQueueName, key => new BlockingCollection<IMessageContext>());
            var queueClient = new QueueClient(commandQueueName, consumerId, onMessagesReceived, queue);
            return queueClient;
        }
    }
}