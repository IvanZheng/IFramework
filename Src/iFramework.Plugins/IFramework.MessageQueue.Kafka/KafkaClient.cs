using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IFramework.Message;
using IFramework.MessageQueue.MSKafka.MessageFormat;
using System.Collections.Concurrent;
using Kafka.Client.Producers;
using Kafka.Client.Consumers;
using IFramework.Infrastructure.Logging;
using IFramework.IoC;
using IFramework.Infrastructure;
using Kafka.Client.Cfg;
using Kafka.Client.Requests;

namespace IFramework.MessageQueue.MSKafka
{
    public class KafkaClient : IMessageQueueClient
    {
        protected ConcurrentDictionary<string, QueueClient> _queueClients;
        protected ConcurrentDictionary<string, TopicClient> _topicClients;
        protected string _zkConnectionString;
        protected ILogger _logger = null;



        #region private methods
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
            var queueClient = new QueueClient(queueName, _zkConnectionString);
            return queueClient;
        }

        TopicClient CreateTopicClient(string topicName)
        {
            ProducerConfiguration producerConfiguration = new ProducerConfiguration(new List<BrokerConfiguration>())
            {
                ZooKeeper = new ZooKeeperConfiguration(_zkConnectionString, 3000, 3000, 3000)
            };
            Producer producer = new Producer(producerConfiguration);
            return new TopicClient(producer);
        }

        SubscriptionClient CreateSubscriptionClient(string topicName, string subscriptionName)
        {
            //TopicDescription topicDescription = new TopicDescription(topicName);
            //if (!_namespaceManager.TopicExists(topicName))
            //{
            //    _namespaceManager.CreateTopic(topicDescription);
            //}

            //if (!_namespaceManager.SubscriptionExists(topicDescription.Path, subscriptionName))
            //{
            //    var subscriptionDescription =
            //        new SubscriptionDescription(topicDescription.Path, subscriptionName);
            //    _namespaceManager.CreateSubscription(subscriptionDescription);
            //}
            //return _messageFactory.CreateSubscriptionClient(topicDescription.Path, subscriptionName);
            return null;
        }
        #endregion


        public KafkaClient()
        {
            _logger = IoCFactory.Resolve<ILoggerFactory>().Create(this.GetType());

        }

        public void CompleteMessage(IMessageContext messageContext)
        {
            (messageContext as MessageContext).Complete();
        }

        public void Publish(IMessageContext messageContext, string topic)
        {
            throw new NotImplementedException();
        }

        public void Send(IMessageContext messageContext, string queue)
        {
            throw new NotImplementedException();
        }

        public void StartQueueClient(string commandQueueName, Action<IMessageContext> onMessageReceived)
        {
            throw new NotImplementedException();
        }

        public void StartSubscriptionClient(string topic, string subscriptionName, Action<IMessageContext> onMessageReceived)
        {
            throw new NotImplementedException();
        }

        public void StopQueueClients()
        {
            throw new NotImplementedException();
        }

        public void StopSubscriptionClients()
        {
            throw new NotImplementedException();
        }

        public IMessageContext WrapMessage(object message, string correlationId = null, string topic = null, string key = null, string replyEndPoint = null, string messageId = null)
        {
            throw new NotImplementedException();
        }
    }
}
