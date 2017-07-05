using System;
using System.Collections.Concurrent;
using System.Threading;
using IFramework.Config;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Logging;
using IFramework.IoC;
using IFramework.Message;
using IFramework.Message.Impl;
using IFramework.MessageQueue.ServiceBus.MessageFormat;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System.Threading.Tasks;

namespace IFramework.MessageQueue.ServiceBus
{
    public class ServiceBusClient: IMessageQueueClient
    {
        protected ILogger _logger;
        protected MessagingFactory _messageFactory;
        protected NamespaceManager _namespaceManager;
        protected ConcurrentDictionary<string, QueueClient> _queueClients;
        protected string _serviceBusConnectionString;
        protected ConcurrentDictionary<string, TopicClient> _topicClients;

        public ServiceBusClient(string serviceBusConnectionString)
        {
            _serviceBusConnectionString = serviceBusConnectionString;
            _namespaceManager = NamespaceManager.CreateFromConnectionString(_serviceBusConnectionString);
            _messageFactory = MessagingFactory.CreateFromConnectionString(_serviceBusConnectionString);
            _topicClients = new ConcurrentDictionary<string, TopicClient>();
            _queueClients = new ConcurrentDictionary<string, QueueClient>();
            _logger = IoCFactory.Resolve<ILoggerFactory>().Create(GetType());
        }

        public async Task PublishAsync(IMessageContext messageContext, string topic, CancellationToken cancellationToken)
        {
            topic = Configuration.Instance.FormatMessageQueueName(topic);
            var topicClient = GetTopicClient(topic);
            var brokeredMessage = ((MessageContext)messageContext).BrokeredMessage;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    await topicClient.SendAsync(brokeredMessage);
                    break;
                }
                catch (InvalidOperationException)
                {
                    brokeredMessage = brokeredMessage.Clone();
                }
            }
        }

        public async Task SendAsync(IMessageContext messageContext, string queue, CancellationToken cancellationToken)
        {
            var commandKey = messageContext.Key;
            queue = Configuration.Instance.FormatMessageQueueName(queue);

            var queuePartitionCount = Configuration.Instance.GetQueuePartitionCount(queue);
            if (queuePartitionCount > 1)
            {
                var keyUniqueCode = !string.IsNullOrWhiteSpace(commandKey)
                                        ? commandKey.GetUniqueCode()
                                        : messageContext.MessageID.GetUniqueCode();
                queue = $"{queue}.{Math.Abs(keyUniqueCode % queuePartitionCount)}";
            }
            else
            {
                queue = $"{queue}.0";
            }
            var queueClient = GetQueueClient(queue);
            var brokeredMessage = ((MessageContext)messageContext).BrokeredMessage;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    await queueClient.SendAsync(brokeredMessage);
                    break;
                }
                catch (InvalidOperationException)
                {
                    brokeredMessage = brokeredMessage.Clone();
                }
            }
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
            var messageContext = new MessageContext(message, messageId);
            messageContext.Producer = producer;
            messageContext.IP = Utility.GetLocalIPV4()?.ToString();
            if (!string.IsNullOrEmpty(correlationId))
            {
                messageContext.CorrelationID = correlationId;
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
            if (sagaInfo != null && !string.IsNullOrWhiteSpace(sagaInfo.SagaId))
            {
                messageContext.SagaInfo = sagaInfo;
            }
            return messageContext;
        }

        public ICommitOffsetable StartQueueClient(string commandQueueName,
                                                  string consumerId,
                                                  OnMessagesReceived onMessagesReceived,
                                                  int fullLoadThreshold = 1000,
                                                  int waitInterval = 1000)
        {
            commandQueueName = $"{commandQueueName}.{consumerId}";
            commandQueueName = Configuration.Instance.FormatMessageQueueName(commandQueueName);
            var commandQueueClient = CreateQueueClient(commandQueueName);
            return new QueueConsumer(commandQueueName, onMessagesReceived, commandQueueClient);
        }

        public ICommitOffsetable StartSubscriptionClient(string topic,
                                                         string subscriptionName,
                                                         string consumerId,
                                                         OnMessagesReceived onMessagesReceived,
                                                         int fullLoadThreshold = 1000,
                                                         int waitInterval = 1000)
        {
            topic = Configuration.Instance.FormatMessageQueueName(topic);
            subscriptionName = Configuration.Instance.FormatMessageQueueName(subscriptionName);
            var subscriptionClient = CreateSubscriptionClient(topic, subscriptionName);
            return new SubscriptionConsumer($"{topic}.{subscriptionName}", onMessagesReceived, subscriptionClient);
        }

        public void Dispose()
        {
            _topicClients.Values.ForEach(client => client.Close());
            _queueClients.Values.ForEach(client => client.Close());
        }

        private TopicClient GetTopicClient(string topic)
        {
            TopicClient topicClient = null;
            _topicClients.TryGetValue(topic, out topicClient);
            if (topicClient == null)
            {
                topicClient = CreateTopicClient(topic);
                _topicClients.GetOrAdd(topic, topicClient);
            }
            return topicClient;
        }

        private QueueClient GetQueueClient(string queue)
        {
            var queueClient = _queueClients.TryGetValue(queue);
            if (queueClient == null)
            {
                queueClient = CreateQueueClient(queue);
                _queueClients.GetOrAdd(queue, queueClient);
            }
            return queueClient;
        }

        private QueueClient CreateQueueClient(string queueName)
        {
            if (!_namespaceManager.QueueExists(queueName))
            {
                _namespaceManager.CreateQueue(queueName);
            }
            return _messageFactory.CreateQueueClient(queueName);
        }

        private TopicClient CreateTopicClient(string topicName)
        {
            var td = new TopicDescription(topicName);
            if (!_namespaceManager.TopicExists(topicName))
            {
                _namespaceManager.CreateTopic(td);
            }
            return _messageFactory.CreateTopicClient(topicName);
        }

        private SubscriptionClient CreateSubscriptionClient(string topicName, string subscriptionName)
        {
            var topicDescription = new TopicDescription(topicName);
            if (!_namespaceManager.TopicExists(topicName))
            {
                _namespaceManager.CreateTopic(topicDescription);
            }

            if (!_namespaceManager.SubscriptionExists(topicDescription.Path, subscriptionName))
            {
                var subscriptionDescription =
                    new SubscriptionDescription(topicDescription.Path, subscriptionName);
                _namespaceManager.CreateSubscription(subscriptionDescription);
            }
            return _messageFactory.CreateSubscriptionClient(topicDescription.Path, subscriptionName);
        }
    }
}