using System.Collections.Concurrent;
using System.Collections.Generic;
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
        protected List<IMessageConsumer> SubscriptionClients;
        protected ConcurrentDictionary<string, IMessageProducer> TopicClients;


        public MessageQueueClient(IMessageQueueClientProvider clientProvider)
        {
            _clientProvider = clientProvider;
            QueueClients = new ConcurrentDictionary<string, IMessageProducer>();
            TopicClients = new ConcurrentDictionary<string, IMessageProducer>();
            SubscriptionClients = new List<IMessageConsumer>();
            QueueConsumers = new List<IMessageConsumer>();
            Logger = IoCFactory.GetService<ILoggerFactory>().CreateLogger(GetType().Name);
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

        public ICommitOffsetable StartQueueClient(string commandQueueName,
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

        public ICommitOffsetable StartSubscriptionClient(string topic,
                                                         string subscriptionName,
                                                         string consumerId,
                                                         OnMessagesReceived onMessagesReceived,
                                                         ConsumerConfig consumerConfig = null)
        {
            topic = Configuration.Instance.FormatMessageQueueName(topic);
            subscriptionName = Configuration.Instance.FormatMessageQueueName(subscriptionName);
            var topicSubscription = _clientProvider.CreateTopicSubscription(topic,
                                                                             subscriptionName,
                                                                             onMessagesReceived,
                                                                             consumerId,
                                                                             consumerConfig);
            SubscriptionClients.Add(topicSubscription);
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
                Disposed = true;
            }
        }


        #region private methods

        private IMessageProducer GetTopicProducer(string topic)
        {
            TopicClients.TryGetValue(topic, out var topicProducer);
            if (topicProducer == null)
            {
                topicProducer = _clientProvider.CreateTopicProducer(topic);
                TopicClients.GetOrAdd(topic, topicProducer);
            }
            return topicProducer;
        }

        private IMessageProducer GetQueueProducer(string queue)
        {
            QueueClients.TryGetValue(queue, out var queueProducer);
            if (queueProducer == null)
            {
                queueProducer = _clientProvider.CreateQueueProducer(queue);
                QueueClients.GetOrAdd(queue, queueProducer);
            }
            return queueProducer;
        }
        #endregion
    }
}