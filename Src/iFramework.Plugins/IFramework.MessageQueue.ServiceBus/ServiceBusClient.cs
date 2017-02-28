using IFramework.Config;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Logging;
using IFramework.IoC;
using IFramework.Message;
using IFramework.Message.Impl;
using IFramework.MessageQueue.ServiceBus.MessageFormat;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Concurrent;

namespace IFramework.MessageQueue.ServiceBus
{
    public class ServiceBusClient : IMessageQueueClient
    {
        protected string _serviceBusConnectionString;
        protected NamespaceManager _namespaceManager;
        protected MessagingFactory _messageFactory;
        protected ConcurrentDictionary<string, TopicClient> _topicClients;
        protected ConcurrentDictionary<string, QueueClient> _queueClients;
        protected ILogger _logger = null;
        public ServiceBusClient(string serviceBusConnectionString)
        {
            _serviceBusConnectionString = serviceBusConnectionString;
            _namespaceManager = NamespaceManager.CreateFromConnectionString(_serviceBusConnectionString);
            _messageFactory = MessagingFactory.CreateFromConnectionString(_serviceBusConnectionString);
            _topicClients = new ConcurrentDictionary<string, TopicClient>();
            _queueClients = new ConcurrentDictionary<string, QueueClient>();
            _logger = IoCFactory.Resolve<ILoggerFactory>().Create(this.GetType());
        }

        TopicClient GetTopicClient(string topic)
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

        QueueClient GetQueueClient(string queue)
        {
            QueueClient queueClient = _queueClients.TryGetValue(queue);
            if (queueClient == null)
            {
                queueClient = CreateQueueClient(queue);
                _queueClients.GetOrAdd(queue, queueClient);
            }
            return queueClient;
        }

        QueueClient CreateQueueClient(string queueName)
        {
            if (!_namespaceManager.QueueExists(queueName))
            {
                _namespaceManager.CreateQueue(queueName);
            }
            return _messageFactory.CreateQueueClient(queueName);
        }

        TopicClient CreateTopicClient(string topicName)
        {
            TopicDescription td = new TopicDescription(topicName);
            if (!_namespaceManager.TopicExists(topicName))
            {
                _namespaceManager.CreateTopic(td);
            }
            return _messageFactory.CreateTopicClient(topicName);
        }

        SubscriptionClient CreateSubscriptionClient(string topicName, string subscriptionName)
        {
            TopicDescription topicDescription = new TopicDescription(topicName);
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

        public void Publish(IMessageContext messageContext, string topic)
        {
            topic = Configuration.Instance.FormatMessageQueueName(topic);
            var topicClient = GetTopicClient(topic);
            var brokeredMessage = ((MessageContext)messageContext).BrokeredMessage;
            while (true)
            {
                try
                {
                    topicClient.Send(brokeredMessage);
                    break;
                }
                catch (InvalidOperationException)
                {
                    brokeredMessage = brokeredMessage.Clone();
                }
            }

        }

        public void Send(IMessageContext messageContext, string queue)
        {
            var commandKey = messageContext.Key;
            queue = Configuration.Instance.FormatMessageQueueName(queue);

            var queuePartitionCount = Configuration.Instance.GetQueuePartitionCount(queue);
            if (queuePartitionCount > 1)
            {
                int keyUniqueCode = !string.IsNullOrWhiteSpace(commandKey) ?
                               commandKey.GetUniqueCode() : messageContext.MessageID.GetUniqueCode();
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
                try
                {
                    queueClient.Send(brokeredMessage);
                    break;
                }
                catch (InvalidOperationException)
                {
                    brokeredMessage = brokeredMessage.Clone();
                }
            }

        }

        public IMessageContext WrapMessage(object message, string correlationId = null,
                                           string topic = null, string key = null,
                                           string replyEndPoint = null, string messageId = null,
                                           SagaInfo sagaInfo = null)
        {
            var messageContext = new MessageContext(message, messageId);
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
        public ICommitOffsetable StartQueueClient(string commandQueueName, string consumerId, OnMessagesReceived onMessagesReceived, int fullLoadThreshold = 1000, int waitInterval = 1000)
        {
            commandQueueName = $"{commandQueueName}.{consumerId}";
            commandQueueName = Configuration.Instance.FormatMessageQueueName(commandQueueName);
            var commandQueueClient = CreateQueueClient(commandQueueName);
            return new QueueConsumer(commandQueueName, onMessagesReceived, commandQueueClient);
        }

        public ICommitOffsetable StartSubscriptionClient(string topic, string subscriptionName, string consumerId, OnMessagesReceived onMessagesReceived, int fullLoadThreshold = 1000, int waitInterval = 1000)
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
    }
}
