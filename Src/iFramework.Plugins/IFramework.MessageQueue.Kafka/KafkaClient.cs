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
        protected List<KafkaConsumer> _subscriptionClients;
        protected List<KafkaConsumer> _queueConsumers;

        protected List<Task> _subscriptionClientTasks;
        protected List<Task> _commandClientTasks;
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

        public void CreateTopic(string topic)
        {
            ProducerConfiguration producerConfiguration = new ProducerConfiguration(new List<BrokerConfiguration>())
            {
                RequiredAcks = -1,
                TotalNumPartitions = 3,
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

        KafkaConsumer CreateQueueConsumer(string queue, string consumerId = null, int fullLoadThreshold = 1000, int waitInterval = 1000)
        {
            CreateTopicIfNotExists(queue);
            var queueConsumer = new KafkaConsumer(_zkConnectionString, queue, $"{queue}.consumer", consumerId, fullLoadThreshold, waitInterval);
            return queueConsumer;
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

        KafkaConsumer CreateSubscriptionClient(string topic, string subscriptionName, string consumerId = null, int fullLoadThreshold = 1000, int waitInterval = 1000)
        {
            CreateTopicIfNotExists(topic);
            return new KafkaConsumer(_zkConnectionString, topic, subscriptionName, consumerId, fullLoadThreshold, waitInterval);
        }

        void ReceiveMessages(CancellationTokenSource cancellationTokenSource, OnMessagesReceived onMessagesReceived, KafkaConsumer kafkaConsumer)
        {
            IEnumerable<Kafka.Client.Messages.Message> messages = null;

            #region peek messages that not been consumed since last time
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    messages = kafkaConsumer.GetMessages(cancellationTokenSource.Token);
                    foreach (var message in messages)
                    {
                        try
                        {
                            var kafkaMessage = Encoding.UTF8.GetString(message.Payload).ToJsonObject<KafkaMessage>();
                            var messageContext = new MessageContext(kafkaMessage, message.PartitionId.Value, message.Offset);
                            kafkaConsumer.AddMessage(message);
                            onMessagesReceived(messageContext);
                            kafkaConsumer.BlockIfFullLoad();
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
                            if (message.Payload != null)
                            {
                                kafkaConsumer.RemoveMessage(message);
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
                    if (!cancellationTokenSource.IsCancellationRequested)
                    {
                        Thread.Sleep(1000);
                        _logger.Error(ex.GetBaseException().Message, ex);
                    }
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
            _subscriptionClients = new List<KafkaConsumer>();
            _queueConsumers = new List<KafkaConsumer>();
            _logger = IoCFactory.Resolve<ILoggerFactory>().Create(this.GetType().Name);
        }



        //public void CompleteMessage(IMessageContext messageContext)
        //{
        //    (messageContext as MessageContext).Complete();
        //    _logger.Debug($"complete message {messageContext.Message.ToJson()}");
        //}


        public void Publish(IMessageContext messageContext, string topic)
        {
            topic = Configuration.Instance.FormatAppName(topic);
            topic = Configuration.Instance.FormatMessageQueueName(topic);
            var topicClient = GetTopicClient(topic);
            var jsonValue = ((MessageContext)messageContext).KafkaMessage.ToJson();
            var message = new Kafka.Client.Messages.Message(Encoding.UTF8.GetBytes(jsonValue));
            var producerData = new ProducerData<string, Kafka.Client.Messages.Message>(topic, messageContext.Key, message);

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
            var producerData = new ProducerData<string, Kafka.Client.Messages.Message>(queue, messageContext.Key, message);

            try
            {
                queueClient.Send(producerData);
            }
            catch (Exception ex)
            {
                _logger.Error($"send message failed: {jsonValue}.", ex);
            }
        }

        public Action<IMessageContext> StartQueueClient(string commandQueueName, string consumerId, OnMessagesReceived onMessagesReceived, int fullLoadThreshold = 1000, int waitInterval = 1000)
        {
            commandQueueName = Configuration.Instance.FormatMessageQueueName(commandQueueName);
            var queueConsumer = CreateQueueConsumer(commandQueueName, consumerId, fullLoadThreshold, waitInterval);
            var cancellationSource = new CancellationTokenSource();
            var task = Task.Factory.StartNew((cs) => ReceiveMessages(cs as CancellationTokenSource,
                                                                          onMessagesReceived,
                                                                          queueConsumer),
                                                     cancellationSource,
                                                     cancellationSource.Token,
                                                     TaskCreationOptions.LongRunning,
                                                     TaskScheduler.Default);
            _commandClientTasks.Add(task);
            _queueConsumers.Add(queueConsumer);
            return queueConsumer.CommitOffset;
        }

        public Action<IMessageContext> StartSubscriptionClient(string topic, string subscriptionName, string consumerId, OnMessagesReceived onMessagesReceived, int fullLoadThreshold = 1000, int waitInterval = 1000)
        {
            topic = Configuration.Instance.FormatMessageQueueName(topic);
            subscriptionName = Configuration.Instance.FormatMessageQueueName(subscriptionName);
            var subscriptionClient = CreateSubscriptionClient(topic, subscriptionName, consumerId, fullLoadThreshold, waitInterval);
            var cancellationSource = new CancellationTokenSource();

            var task = Task.Factory.StartNew((cs) => ReceiveMessages(cs as CancellationTokenSource,
                                                                   onMessagesReceived,
                                                                   subscriptionClient),
                                             cancellationSource,
                                             cancellationSource.Token,
                                             TaskCreationOptions.LongRunning,
                                             TaskScheduler.Default);
            _subscriptionClientTasks.Add(task);
            _subscriptionClients.Add(subscriptionClient);
            return subscriptionClient.CommitOffset;
        }

        void StopQueueClients()
        {
            _commandClientTasks.ForEach(task =>
            {
                CancellationTokenSource cancellationSource = task.AsyncState as CancellationTokenSource;
                cancellationSource.Cancel(true);
            }
            );
            _queueConsumers.ForEach(client => client.Stop());
            Task.WaitAll(_commandClientTasks.ToArray());
        }

        void StopSubscriptionClients()
        {
            _subscriptionClientTasks.ForEach(subscriptionClientTask =>
            {
                CancellationTokenSource cancellationSource = subscriptionClientTask.AsyncState as CancellationTokenSource;
                cancellationSource.Cancel(true);
            }
              );
            _subscriptionClients.ForEach(client => client.Stop());
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

        public void Dispose()
        {
            StopQueueClients();
            StopSubscriptionClients();
            _topicClients.Values.ForEach(client => client.Stop());
            _queueClients.Values.ForEach(client => client.Stop());
        }
    }
}
