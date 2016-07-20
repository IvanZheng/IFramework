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
using IFramework.Config;
using System.Threading;
using Kafka.Client.Helper;

namespace IFramework.MessageQueue.MSKafka
{
    public class KafkaClient : IMessageQueueClient
    {
        protected ConcurrentDictionary<string, QueueClient> _queueClients;
        protected ConcurrentDictionary<string, TopicClient> _topicClients;
        protected List<Task> _subscriptionClientTasks;
        protected List<Task> _commandClientTasks;
        protected string _zkConnectionString;
        protected ILogger _logger = null;

        #region private methods
        public TopicClient GetTopicClient(string topic)
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

        public QueueClient GetQueueClient(string queue)
        {
            QueueClient queueClient = _queueClients.TryGetValue(queue);
            if (queueClient == null)
            {
                queueClient = CreateQueueClient(queue);
                _queueClients.GetOrAdd(queue, queueClient);
            }
            return queueClient;
        }

        bool TopicExsits(string topic)
        {
            var managerConfig = new KafkaSimpleManagerConfiguration()
            {
                FetchSize = KafkaSimpleManagerConfiguration.DefaultFetchSize,
                BufferSize = KafkaSimpleManagerConfiguration.DefaultBufferSize,
                Zookeeper = _zkConnectionString
            };
            var kafkaManager = new KafkaSimpleManager<string, Kafka.Client.Messages.Message>(managerConfig);
            try
            {
                // get all available partitions for a topic through the manager
                var allPartitions = kafkaManager.GetTopicPartitionsFromZK(topic);
                return allPartitions.Count > 0;
            }
            catch (Exception)
            {
                return false;
            }
           
        }

        void CreateTopic(string topic)
        {
            ProducerConfiguration producerConfiguration = new ProducerConfiguration(new List<BrokerConfiguration>())
            {
                RequiredAcks = 1,
                ZooKeeper = new ZooKeeperConfiguration(_zkConnectionString, 3000, 3000, 3000)
            };
            using (Producer producer = new Producer(producerConfiguration))
            {
                try
                {
                    var data = new ProducerData<string, Kafka.Client.Messages.Message>(topic, string.Empty, new Kafka.Client.Messages.Message(new byte[0]));
                    producer.Send(data);
                }
                catch (Exception ex)
                {
                    _logger.Error($"Create topic {topic} failed", ex);
                }
            } 
        }

        void CreateTopicIfNotExists(string topic)
        {
            if (!TopicExsits(topic))
            {
                CreateTopic(topic);
            }
        }

        QueueClient CreateQueueClient(string queue)
        {
            CreateTopicIfNotExists(queue);
            var queueClient = new QueueClient(queue, _zkConnectionString);
            return queueClient;
        }

        TopicClient CreateTopicClient(string topic)
        {
            CreateTopicIfNotExists(topic);
            return new TopicClient(topic, _zkConnectionString);
        }

        SubscriptionClient CreateSubscriptionClient(string topic, string subscriptionName)
        {
            CreateTopicIfNotExists(topic);
            return new SubscriptionClient(topic, subscriptionName, _zkConnectionString);
        }

        void CompleteTopicMessage(SubscriptionClient subscriptionClient, long offset)
        {
            try
            {
                subscriptionClient.CommitOffset(offset);
            }
            catch (Exception ex)
            {
                _logger.Error(ex.GetBaseException().Message, ex);
            }
        }

        void CompleteQueueMessage(QueueClient queueClient, long offset)
        {
            try
            {
                queueClient.CommitOffset(offset);
            }
            catch (Exception ex)
            {
                _logger.Error(ex.GetBaseException().Message, ex);
            }
        }

        void ReceiveTopicMessages(CancellationTokenSource cancellationSource, Action<IMessageContext> onMessageReceived, SubscriptionClient subscriptionClient)
        {
            while (!cancellationSource.IsCancellationRequested)
            {
                try
                {
                    IEnumerable<Kafka.Client.Messages.Message> messages = subscriptionClient.ReceiveMessages(cancellationSource.Token);
                    messages.ForEach(message =>
                    {
                        try
                        {
                            var kafkaMessage = Encoding.UTF8.GetString(message.Payload).ToJsonObject<KafkaMessage>();

                            var eventContext = new MessageContext(kafkaMessage, message.Offset);
                            onMessageReceived(eventContext);
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
                            _logger.Error(ex.GetBaseException().Message, ex);
                        }
                        finally
                        {
                            if (message.Payload != null)
                            {
                                CompleteTopicMessage(subscriptionClient, message.Offset);
                            }
                        }
                    });
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

        void ReceiveQueueMessages(CancellationTokenSource cancellationTokenSource, Action<IMessageContext> onMessageReceived, QueueClient queueClient)
        {
            IEnumerable<Kafka.Client.Messages.Message> kafkaMessages = null;

            #region peek messages that not been consumed since last time
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    kafkaMessages = queueClient.PeekBatch(cancellationTokenSource.Token);
                    //if (kafkaMessages == null || kafkaMessages.Count() == 0)
                    //{
                    //    break;
                    //}
                    foreach (var kafkaMessage in kafkaMessages)
                    {
                        try
                        {
                            var message = Encoding.UTF8.GetString(kafkaMessage.Payload).ToJsonObject<KafkaMessage>();
                            onMessageReceived(new MessageContext(message,
                                                                 kafkaMessage.Offset,
                                                                () => CompleteQueueMessage(queueClient, kafkaMessage.Offset)));
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
                            if (kafkaMessage.Offset > 0)
                            {
                                CompleteQueueMessage(queueClient, kafkaMessage.Offset);
                            }
                            _logger.Error(ex.GetBaseException().Message, ex);
                        }
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
        #endregion


        public KafkaClient(string zkConnectionString)
        {
            _zkConnectionString = zkConnectionString;
            _queueClients = new ConcurrentDictionary<string, QueueClient>();
            _topicClients = new ConcurrentDictionary<string, TopicClient>();
            _subscriptionClientTasks = new List<Task>();
            _commandClientTasks = new List<Task>();
            _logger = IoCFactory.Resolve<ILoggerFactory>().Create(this.GetType().Name);

        }

        public void CompleteMessage(IMessageContext messageContext)
        {
            (messageContext as MessageContext).Complete();
            _logger.Debug($"complete message {messageContext.Message.ToJson()}");
        }

        public void Publish(IMessageContext messageContext, string topic)
        {
            topic = Configuration.Instance.FormatMessageQueueName(topic);
            var topicClient = GetTopicClient(topic);
            var jsonValue = ((MessageContext)messageContext).KafkaMessage.ToJson();
            var message = new Kafka.Client.Messages.Message(Encoding.UTF8.GetBytes(jsonValue));
            var producerData = new ProducerData<string, Kafka.Client.Messages.Message>(topic, message);

            try
            {
                topicClient.Send(producerData);
            }
            catch (Exception ex)
            {
                _logger.Error($"send message failed: {jsonValue}.", ex);
            }
        }

        public void Send(IMessageContext messageContext, string queue)
        {
            queue = Configuration.Instance.FormatMessageQueueName(queue);
            var queueClient = GetQueueClient(queue);

            var jsonValue = ((MessageContext)messageContext).KafkaMessage.ToJson();
            var message = new Kafka.Client.Messages.Message(Encoding.UTF8.GetBytes(jsonValue));
            var producerData = new ProducerData<string, Kafka.Client.Messages.Message>(queue, message);

            try
            {
                queueClient.Send(producerData);
            }
            catch (Exception ex)
            {
                _logger.Error($"send message failed: {jsonValue}.", ex);
            }
        }

        public void StartQueueClient(string commandQueueName, Action<IMessageContext> onMessageReceived)
        {
            commandQueueName = Configuration.Instance.FormatMessageQueueName(commandQueueName);
            var commandQueueClient = GetQueueClient(commandQueueName);
            var cancellationSource = new CancellationTokenSource();
            var task = Task.Factory.StartNew((cs) => ReceiveQueueMessages(cs as CancellationTokenSource,
                                                                          onMessageReceived,
                                                                          commandQueueClient),
                                                     cancellationSource,
                                                     cancellationSource.Token,
                                                     TaskCreationOptions.LongRunning,
                                                     TaskScheduler.Default);
            _commandClientTasks.Add(task);
        }

        public void StartSubscriptionClient(string topic, string subscriptionName, Action<IMessageContext> onMessageReceived)
        {
            topic = Configuration.Instance.FormatMessageQueueName(topic);
            subscriptionName = Configuration.Instance.FormatMessageQueueName(subscriptionName);
            var subscriptionClient = CreateSubscriptionClient(topic, subscriptionName);
            var cancellationSource = new CancellationTokenSource();

            var task = Task.Factory.StartNew((cs) => ReceiveTopicMessages(cs as CancellationTokenSource,
                                                                   onMessageReceived,
                                                                   subscriptionClient),
                                             cancellationSource,
                                             cancellationSource.Token,
                                             TaskCreationOptions.LongRunning,
                                             TaskScheduler.Default);
            _subscriptionClientTasks.Add(task);
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

        public IMessageContext WrapMessage(object message, string correlationId = null, string topic = null, string key = null, string replyEndPoint = null, string messageId = null)
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

    }
}
