using IFramework.Infrastructure;
using IFramework.Infrastructure.Logging;
using IFramework.Message;
using IFramework.MessageQueue.ServiceBus.MessageFormat;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IFramework.MessageQueue.ServiceBus
{
    public class ServiceBusClient : IMessageQueueClient
    {
        protected string _serviceBusConnectionString;
        protected NamespaceManager _namespaceManager;
        protected MessagingFactory _messageFactory;
        protected ConcurrentDictionary<string, TopicClient> _topicClients;
        protected ConcurrentDictionary<string, QueueClient> _queueClients;

        protected List<Task> _subscriptionClientTasks;
        protected List<Task> _commandClientTasks;
        protected ILogger _logger = null;
        public ServiceBusClient(string serviceBusConnectionString)
        {
            _serviceBusConnectionString = serviceBusConnectionString;
            _namespaceManager = NamespaceManager.CreateFromConnectionString(_serviceBusConnectionString);
            _messageFactory = MessagingFactory.CreateFromConnectionString(_serviceBusConnectionString);
            _topicClients = new ConcurrentDictionary<string, TopicClient>();
            _queueClients = new ConcurrentDictionary<string, QueueClient>();
            _subscriptionClientTasks = new List<Task>();
            _commandClientTasks = new List<Task>();
            _logger = IoCFactory.Resolve<ILoggerFactory>().Create(this.GetType());
        }

        public void CloseTopicClients()
        {
            _topicClients.Values.ForEach(client => client.Close());
        }
        internal TopicClient GetTopicClient(string topic)
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

        internal QueueClient GetQueueClient(string queue)
        {
            QueueClient queueClient = _queueClients.TryGetValue(queue);
            if (queueClient == null)
            {
                queueClient = CreateQueueClient(queue);
                _queueClients.GetOrAdd(queue, queueClient);
            }
            return queueClient;
        }

        internal QueueClient CreateQueueClient(string queueName)
        {
            if (!_namespaceManager.QueueExists(queueName))
            {
                _namespaceManager.CreateQueue(queueName);
            }
            return _messageFactory.CreateQueueClient(queueName);
        }

        internal TopicClient CreateTopicClient(string topicName)
        {
            TopicDescription td = new TopicDescription(topicName);
            if (!_namespaceManager.TopicExists(topicName))
            {
                _namespaceManager.CreateTopic(td);
            }
            return _messageFactory.CreateTopicClient(topicName);
        }

        internal SubscriptionClient CreateSubscriptionClient(string topicName, string subscriptionName)
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
            var topicClient = GetTopicClient(topic);
            topicClient.Send(((MessageContext)messageContext).BrokeredMessage);
        }

        public void Send(IMessageContext messageContext, string queue)
        {
            var queueClient = GetQueueClient(queue);
            queueClient.Send(((MessageContext)messageContext).BrokeredMessage);
        }

        public IMessageContext WrapMessage(object message, string correlationId = null, string topic = null)
        {
            var messageContext = new MessageContext(message);
            if (!string.IsNullOrEmpty(correlationId))
            {
                messageContext.CorrelationID = correlationId;
            }
            if (!string.IsNullOrEmpty(topic))
            {
                messageContext.Topic = topic;
            }
            return messageContext;
        }


        public void StartSubscriptionClient(string topic, string subscriptionName, Action<IMessageContext> onMessageReceived)
        {
            var subscriptionClient = CreateSubscriptionClient(topic, subscriptionName);
            var cancellationSource = new CancellationTokenSource();

            var task = Task.Factory.StartNew(() => ReceiveMessages(cancellationSource,
                                                                   onMessageReceived,
                                                                   () => subscriptionClient.Receive(new TimeSpan(0, 0, 2))),
                                             cancellationSource.Token,
                                             TaskCreationOptions.LongRunning,
                                             TaskScheduler.Default);
            _subscriptionClientTasks.Add(task);
        }

        public void StopSubscriptionClients()
        {
            _subscriptionClientTasks.ForEach(subscriptionClientTask =>
                {
                    CancellationTokenSource cancellationSource = ((dynamic)(subscriptionClientTask.AsyncState)).CancellationSource;
                    cancellationSource.Cancel(true);
                }
            );
            Task.WaitAll(_subscriptionClientTasks.ToArray());
        }

        private void ReceiveMessages(CancellationTokenSource cancellationSource, Action<IMessageContext> onMessageReceived, Func<BrokeredMessage> receiveMessage)
        {
            while (!cancellationSource.IsCancellationRequested)
            {
                try
                {
                    BrokeredMessage brokeredMessage = null;
                    brokeredMessage = receiveMessage();
                    if (brokeredMessage != null)
                    {
                        var eventContext = new MessageContext(brokeredMessage);
                        onMessageReceived(eventContext);
                        brokeredMessage.Complete();
                    }
                }
                catch (ThreadAbortException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    Thread.Sleep(1000);
                    _logger.Error(ex.GetBaseException().Message, ex);
                }
            }
        }


        public void StartQueueClient(string commandQueueName, Action<IMessageContext> onMessageReceived)
        {

            var commandQueueClient = CreateQueueClient(commandQueueName);
            var cancellationSource = new CancellationTokenSource();
            var task = Task.Factory.StartNew(() => ReceiveMessages(cancellationSource,
                                                                   onMessageReceived,
                                                                   () => commandQueueClient.Receive(new TimeSpan(0, 0, 2))),
                                             cancellationSource.Token,
                                             TaskCreationOptions.LongRunning,
                                             TaskScheduler.Default);
            _commandClientTasks.Add(task);
        }

        public void StopQueueClients()
        {
            _commandClientTasks.ForEach(task =>
            {
                CancellationTokenSource cancellationSource = ((dynamic)(task.AsyncState)).CancellationSource;
                cancellationSource.Cancel(true);
            }
           );
            Task.WaitAll(_commandClientTasks.ToArray());
        }
    }
}
