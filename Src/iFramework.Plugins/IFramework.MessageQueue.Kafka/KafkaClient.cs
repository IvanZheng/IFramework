using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using IFramework.Message;
using IFramework.MessageQueue.MSKafka.MessageFormat;
using System.Collections.Concurrent;
using Kafka.Client.Producers;
using IFramework.Infrastructure.Logging;
using IFramework.IoC;
using IFramework.Infrastructure;
using Kafka.Client.Cfg;
using IFramework.Config;
using System.Threading;
using Kafka.Client.Helper;
using IFramework.MessageQueue.MSKafka.Config;
using Kafka.Client.Consumers;
using IFramework.Message.Impl;

namespace IFramework.MessageQueue.MSKafka
{
    public class KafkaClient : IMessageQueueClient
    {
        protected ConcurrentDictionary<string, KafkaProducer> _queueClients;
        protected ConcurrentDictionary<string, KafkaProducer> _topicClients;
        protected List<KafkaConsumer> _subscriptionClients;
        protected List<KafkaConsumer> _queueConsumers;
        protected string _zkConnectionString;
        protected ILogger _logger = null;

        #region private methods
        KafkaProducer GetTopicClient(string topic)
        {
            KafkaProducer topicClient = null;
            _topicClients.TryGetValue(topic, out topicClient);
            if (topicClient == null)
            {
                topicClient = CreateTopicClient(topic);
                _topicClients.GetOrAdd(topic, topicClient);
            }
            return topicClient;
        }

        KafkaProducer GetQueueClient(string queue)
        {
            KafkaProducer queueClient = _queueClients.TryGetValue(queue);
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
            using (var kafkaManager = new KafkaSimpleManager<string, Kafka.Client.Messages.Message>(managerConfig))
            {
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
        }

        internal static ZooKeeperConfiguration GetZooKeeperConfiguration(string connectString, int sessionTimeout = 30000, int connectionTimeout = 4000, int syncTimeout = 8000)
        {
            return new ZooKeeperConfiguration(connectString, sessionTimeout, connectionTimeout, syncTimeout);
        }

        public void CreateTopic(string topic)
        {
            ProducerConfiguration producerConfiguration = new ProducerConfiguration(new List<BrokerConfiguration>())
            {
                RequiredAcks = -1,
                TotalNumPartitions = 3,
                ZooKeeper = GetZooKeeperConfiguration(_zkConnectionString)
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

        KafkaConsumer CreateQueueConsumer(string queue, OnMessagesReceived onMessagesReceived, 
                                          string consumerId = null, int backOffIncrement = 30, 
                                          int fullLoadThreshold = 1000, int waitInterval = 1000)
        {
            CreateTopicIfNotExists(queue);
            var queueConsumer = new KafkaConsumer(_zkConnectionString, queue, $"{queue}.consumer", consumerId,
                                                  BuildOnKafkaMessageReceived(onMessagesReceived),
                                                  backOffIncrement, fullLoadThreshold, waitInterval);
            return queueConsumer;
        }

        KafkaProducer CreateQueueClient(string queue)
        {
            CreateTopicIfNotExists(queue);
            var queueClient = new KafkaProducer(queue, _zkConnectionString);
            return queueClient;
        }

        KafkaProducer CreateTopicClient(string topic)
        {
            CreateTopicIfNotExists(topic);
            return new KafkaProducer(topic, _zkConnectionString);
        }

        KafkaConsumer CreateSubscriptionClient(string topic, string subscriptionName,
                                               OnMessagesReceived onMessagesReceived,
                                               string consumerId = null, int backOffIncrement = 30, 
                                               int fullLoadThreshold = 1000, int waitInterval = 1000)
        {
            CreateTopicIfNotExists(topic);
            return new KafkaConsumer(_zkConnectionString, topic, subscriptionName, consumerId, 
                                     BuildOnKafkaMessageReceived(onMessagesReceived), backOffIncrement, fullLoadThreshold, waitInterval);
        }

        OnKafkaMessageReceived BuildOnKafkaMessageReceived(OnMessagesReceived onMessagesReceived)
        {
            return (consumer, message) =>
            {
                var kafkaMessage = Encoding.UTF8.GetString(message.Payload).ToJsonObject<KafkaMessage>();
                var messageContext = new MessageContext(kafkaMessage, message.PartitionId.Value, message.Offset);
                onMessagesReceived(messageContext);
            };
        }

        #endregion


        public KafkaClient(string zkConnectionString)
        {
            _zkConnectionString = zkConnectionString;
            _queueClients = new ConcurrentDictionary<string, KafkaProducer>();
            _topicClients = new ConcurrentDictionary<string, KafkaProducer>();
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
            topic = Configuration.Instance.FormatMessageQueueName(topic);
            var topicClient = GetTopicClient(topic);
            var jsonValue = ((MessageContext)messageContext).KafkaMessage.ToJson();
            var message = new Kafka.Client.Messages.Message(Encoding.UTF8.GetBytes(jsonValue));
            var producerData = new ProducerData<string, Kafka.Client.Messages.Message>(topic, messageContext.Key, message);
            topicClient.Send(producerData);
        }

        public void Send(IMessageContext messageContext, string queue)
        {
            queue = Configuration.Instance.FormatMessageQueueName(queue);
            var queueClient = GetQueueClient(queue);

            var jsonValue = ((MessageContext)messageContext).KafkaMessage.ToJson();
            var message = new Kafka.Client.Messages.Message(Encoding.UTF8.GetBytes(jsonValue));
            var producerData = new ProducerData<string, Kafka.Client.Messages.Message>(queue, messageContext.Key, message);
            queueClient.Send(producerData);
        }

        public Action<IMessageContext> StartQueueClient(string commandQueueName, string consumerId, OnMessagesReceived onMessagesReceived, int fullLoadThreshold = 1000, int waitInterval = 1000)
        {
            commandQueueName = Configuration.Instance.FormatMessageQueueName(commandQueueName);
            consumerId = Configuration.Instance.FormatMessageQueueName(consumerId);
            var queueConsumer = CreateQueueConsumer(commandQueueName, onMessagesReceived, consumerId, Configuration.Instance.GetBackOffIncrement(), fullLoadThreshold, waitInterval);
            _queueConsumers.Add(queueConsumer);
            return queueConsumer.CommitOffset;
        }

        public Action<IMessageContext> StartSubscriptionClient(string topic, string subscriptionName, string consumerId, OnMessagesReceived onMessagesReceived, int fullLoadThreshold = 1000, int waitInterval = 1000)
        {
            topic = Configuration.Instance.FormatMessageQueueName(topic);
            subscriptionName = Configuration.Instance.FormatMessageQueueName(subscriptionName);
            var subscriptionClient = CreateSubscriptionClient(topic, subscriptionName, onMessagesReceived, consumerId, Configuration.Instance.GetBackOffIncrement(), fullLoadThreshold, waitInterval);
            _subscriptionClients.Add(subscriptionClient);
            return subscriptionClient.CommitOffset;
        }

        void StopQueueClients()
        {
            _queueConsumers.ForEach(client => client.Stop());
        }

        void StopSubscriptionClients()
        {
            _subscriptionClients.ForEach(client => client.Stop());
        }

        public IMessageContext WrapMessage(object message, string correlationId = null, string topic = null, string key = null, string replyEndPoint = null, string messageId = null, SagaInfo sagaInfo = null)
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

        protected bool _disposed = false;
        public void Dispose()
        {
            if (!_disposed)
            {
                ZookeeperConsumerConnector.zkClientStatic.Dispose();
                StopQueueClients();
                StopSubscriptionClients();
                _topicClients.Values.ForEach(client => client.Stop());
                _queueClients.Values.ForEach(client => client.Stop());
                _disposed = true;
            }
        }
    }
}
