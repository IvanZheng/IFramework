using IFramework.Infrastructure;
using IFramework.Infrastructure.Logging;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.MessageQueue.ServiceBus
{
    public class MessageProcessor
    {
        protected string _serviceBusConnectionString;
        protected bool _exit = false;
        protected readonly ILogger _logger;
        protected NamespaceManager _namespaceManager;
        protected MessagingFactory _messageFactory;
        protected ConcurrentDictionary<string, TopicClient> _topicClients;
        public MessageProcessor(string serviceBusConnectionString)
        {
            _serviceBusConnectionString = serviceBusConnectionString;
            _logger = IoCFactory.Resolve<ILoggerFactory>().Create(this.GetType());
            _namespaceManager = NamespaceManager.CreateFromConnectionString(_serviceBusConnectionString);
            _messageFactory = MessagingFactory.CreateFromConnectionString(_serviceBusConnectionString);
            _topicClients = new ConcurrentDictionary<string, TopicClient>();
        }

        protected void CloseTopicClients()
        {
            _topicClients.Values.ForEach(client => client.Close());
        }
        protected TopicClient GetTopicClient(string topic)
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

        protected QueueClient CreateQueueClient(string queueName)
        {
            if (!_namespaceManager.QueueExists(queueName))
            {
                _namespaceManager.CreateQueue(queueName);
            }
            return _messageFactory.CreateQueueClient(queueName);
        }

        private TopicClient CreateTopicClient(string topicName)
        {
            TopicDescription td = new TopicDescription(topicName);
            if (!_namespaceManager.TopicExists(topicName))
            {
                _namespaceManager.CreateTopic(td);
            }
            return _messageFactory.CreateTopicClient(topicName);
        }

        protected SubscriptionClient CreateSubscriptionClient(string topicName, string subscriptionName)
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
    }
}
