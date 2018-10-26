using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.Infrastructure;
using IFramework.Message;
using IFramework.Message.Impl;
using Microsoft.Extensions.Logging;

namespace IFramework.MessageQueue.Client.Abstracts
{
    public class MessageQueueClient : IMessageQueueClient
    {
        private readonly IMessageQueueClientProvider _clientProvider;
        protected string BrokerList;
        protected bool Disposed;
        protected ILogger Logger;
        protected ConcurrentDictionary<string, IMessageProducer> QueueClients;
        protected List<IMessageConsumer> QueueConsumers;
        protected List<IMessageConsumer> Subscribers;
        protected ConcurrentDictionary<string, IMessageProducer> TopicClients;


        public MessageQueueClient(IMessageQueueClientProvider clientProvider)
        {
            _clientProvider = clientProvider;
            QueueClients = new ConcurrentDictionary<string, IMessageProducer>();
            TopicClients = new ConcurrentDictionary<string, IMessageProducer>();
            Subscribers = new List<IMessageConsumer>();
            QueueConsumers = new List<IMessageConsumer>();
            Logger = ObjectProviderFactory.GetService<ILoggerFactory>().CreateLogger(GetType().Name);
        }


        public virtual Task PublishAsync(IMessageContext messageContext, string topic, CancellationToken cancellationToken)
        {
            topic = Configuration.Instance.FormatMessageQueueName(topic);
            var topicClient = GetTopicProducer(topic);
            return topicClient.SendAsync(messageContext, cancellationToken);
        }

        public virtual Task SendAsync(IMessageContext messageContext, string queue, CancellationToken cancellationToken)
        {
            queue = Configuration.Instance.FormatMessageQueueName(queue);
            var queueClient = GetQueueProducer(queue);
            return queueClient.SendAsync(messageContext, cancellationToken);
        }

        public IMessageConsumer StartQueueClient(string commandQueueName,
                                                  string consumerId,
                                                  OnMessagesReceived onMessagesReceived,
                                                  ConsumerConfig consumerConfig = null)
        {
            commandQueueName = Configuration.Instance.FormatMessageQueueName(commandQueueName);
            consumerId = Configuration.Instance.FormatMessageQueueName(consumerId);
            var queueConsumer = _clientProvider.CreateQueueConsumer(commandQueueName,
                                                                    onMessagesReceived,
                                                                    consumerId,
                                                                    consumerConfig);
            QueueConsumers.Add(queueConsumer);
            return queueConsumer;
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
            subscriptionName = Configuration.Instance.FormatMessageQueueName(subscriptionName);
            var topicSubscription = _clientProvider.CreateTopicSubscription(topics,
                                                                             subscriptionName,
                                                                             onMessagesReceived,
                                                                             consumerId,
                                                                             consumerConfig);
            Subscribers.Add(topicSubscription);
            return topicSubscription;
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
            return _clientProvider.WrapMessage(message,
                                               correlationId,
                                               topic,
                                               key,
                                               replyEndPoint,
                                               messageId,
                                               sagaInfo,
                                               producer);
        }

        public void Dispose()
        {
            if (!Disposed)
            {
                TopicClients.Values.ForEach(client => client.Stop());
                QueueClients.Values.ForEach(client => client.Stop());
                QueueConsumers.ForEach(consumer => consumer.Stop());
                Subscribers.ForEach(subscriber => subscriber.Stop());
                Disposed = true;
            }
        }


        #region private methods

        private IMessageProducer GetTopicProducer(string topic, ProducerConfig config = null)
        {
            return TopicClients.GetOrAdd(topic, key => _clientProvider.CreateTopicProducer(key, config));
        }

        private IMessageProducer GetQueueProducer(string queue, ProducerConfig config = null)
        {
            return QueueClients.GetOrAdd(queue, key => _clientProvider.CreateQueueProducer(key, config));
        }
        #endregion
    }
}