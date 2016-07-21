using IFramework.Config;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Logging;
using IFramework.IoC;
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
            queue = Configuration.Instance.FormatMessageQueueName(queue);
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
                                           string replyEndPoint = null, string messageId = null)
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
            return messageContext;
        }
        public Action<long> StartQueueClient(string commandQueueName, Action<IMessageContext> onMessageReceived)
        {
            commandQueueName = Configuration.Instance.FormatMessageQueueName(commandQueueName);
            var commandQueueClient = CreateQueueClient(commandQueueName);
            var cancellationSource = new CancellationTokenSource();
            var task = Task.Factory.StartNew((cs) => ReceiveQueueMessages(cs as CancellationTokenSource,
                                                                          onMessageReceived,
                                                                          commandQueueClient),
                                                     cancellationSource,
                                                     cancellationSource.Token,
                                                     TaskCreationOptions.LongRunning,
                                                     TaskScheduler.Default);
            _commandClientTasks.Add(task);
            return (offset) => CompleteMessage(commandQueueClient, offset);
        }

        public void StopQueueClients()
        {
            _commandClientTasks.ForEach(task =>
            {
                CancellationTokenSource cancellationSource = task.AsyncState as CancellationTokenSource;
                cancellationSource.Cancel(true);
            }
           );
            Task.WaitAll(_commandClientTasks.ToArray());
        }

        public void StartSubscriptionClient(string topic, string subscriptionName, Action<IMessageContext> onMessageReceived)
        {
            topic = Configuration.Instance.FormatMessageQueueName(topic);
            subscriptionName = Configuration.Instance.FormatMessageQueueName(subscriptionName);
            var subscriptionClient = CreateSubscriptionClient(topic, subscriptionName);
            var cancellationSource = new CancellationTokenSource();

            var task = Task.Factory.StartNew((cs) => ReceiveTopicMessages(cs as CancellationTokenSource,
                                                                   onMessageReceived,
                                                                   () => subscriptionClient.Receive(Configuration.Instance.GetMessageQueueReceiveMessageTimeout())),
                                             cancellationSource,
                                             cancellationSource.Token,
                                             TaskCreationOptions.LongRunning,
                                             TaskScheduler.Default);
            _subscriptionClientTasks.Add(task);
        }

        public void CompleteMessage(IMessageContext messageContext)
        {
            (messageContext as MessageContext).Complete();
        }

        public void StopSubscriptionClients()
        {
            _subscriptionClientTasks.ForEach(subscriptionClientTask =>
                {
                    CancellationTokenSource cancellationSource = subscriptionClientTask.AsyncState as CancellationTokenSource;
                    cancellationSource.Cancel(true);
                }
            );
            Task.WaitAll(_subscriptionClientTasks.ToArray());
        }

        private void ReceiveTopicMessages(CancellationTokenSource cancellationSource, Action<IMessageContext> onMessageReceived, Func<BrokeredMessage> receiveMessage)
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
                catch (OperationCanceledException)
                {
                    return;
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

        private void ReceiveQueueMessages(CancellationTokenSource cancellationTokenSource, Action<IMessageContext> onMessageReceived, QueueClient queueClient)
        {
            bool needPeek = true;
            long sequenceNumber = 0;
            IEnumerable<BrokeredMessage> brokeredMessages = null;

            #region peek messages that not been consumed since last time
            while (!cancellationTokenSource.IsCancellationRequested && needPeek)
            {
                try
                {
                    brokeredMessages = queueClient.PeekBatch(sequenceNumber, 50);
                    if (brokeredMessages == null || brokeredMessages.Count() == 0)
                    {
                        break;
                    }
                    foreach (var message in brokeredMessages)
                    {
                        if (message.State != Microsoft.ServiceBus.Messaging.MessageState.Deferred)
                        {
                            needPeek = false;
                            break;
                        }
                        onMessageReceived(new MessageContext(message));
                        sequenceNumber = message.SequenceNumber + 1;
                    }
                }
                catch (OperationCanceledException)
                {
                    return;
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
            #endregion

            #region receive messages to enqueue consuming queue
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    brokeredMessages = queueClient.ReceiveBatch(50, Configuration.Instance.GetMessageQueueReceiveMessageTimeout());
                    foreach (var message in brokeredMessages)
                    {
                        message.Defer();
                        onMessageReceived(new MessageContext(message));
                    }
                }
                catch (OperationCanceledException)
                {
                    return;
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
            #endregion
        }

        private void CompleteMessage(QueueClient queueClient, long sequenceNumber)
        {
            try
            {
                var toCompleteMessage = queueClient.Receive(sequenceNumber);
                toCompleteMessage.Complete();
            }
            catch (Exception ex)
            {
                _logger.Error(ex.GetBaseException().Message, ex);
            }

        }
    }
}
