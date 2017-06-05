using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using IFramework.Config;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Logging;
using IFramework.IoC;
using IFramework.Message;
using IFramework.Message.Impl;
using IFramework.MessageQueue.ConfluentKafka.Config;
using IFramework.MessageQueue.ConfluentKafka.MessageFormat;

namespace IFramework.MessageQueue.ConfluentKafka
{
    public class ConfluentKafkaClient: IMessageQueueClient
    {
        protected bool _disposed;
        protected ILogger _logger;
        protected ConcurrentDictionary<string, KafkaProducer> _queueClients;
        protected List<KafkaConsumer> _queueConsumers;
        protected List<KafkaConsumer> _subscriptionClients;
        protected ConcurrentDictionary<string, KafkaProducer> _topicClients;
        protected string _brokerList;


        public ConfluentKafkaClient(string brokerList)
        {
            _brokerList = brokerList;
            _queueClients = new ConcurrentDictionary<string, KafkaProducer>();
            _topicClients = new ConcurrentDictionary<string, KafkaProducer>();
            _subscriptionClients = new List<KafkaConsumer>();
            _queueConsumers = new List<KafkaConsumer>();
            _logger = IoCFactory.Resolve<ILoggerFactory>().Create(GetType().Name);
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
            var message = ((MessageContext) messageContext).KafkaMessage;
            topicClient.Send(messageContext.Key, message);
        }

        public void Send(IMessageContext messageContext, string queue)
        {
            queue = Configuration.Instance.FormatMessageQueueName(queue);
            var queueClient = GetQueueClient(queue);

            var message = ((MessageContext) messageContext).KafkaMessage;
            queueClient.Send(messageContext.Key, message);
        }

        public ICommitOffsetable StartQueueClient(string commandQueueName,
                                                  string consumerId,
                                                  OnMessagesReceived onMessagesReceived,
                                                  int fullLoadThreshold = 1000,
                                                  int waitInterval = 1000)
        {
            commandQueueName = Configuration.Instance.FormatMessageQueueName(commandQueueName);
            consumerId = Configuration.Instance.FormatMessageQueueName(consumerId);
            var queueConsumer = CreateQueueConsumer(commandQueueName, onMessagesReceived, consumerId,
                                                    Configuration.Instance.GetBackOffIncrement(), fullLoadThreshold, waitInterval);
            _queueConsumers.Add(queueConsumer);
            return queueConsumer;
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
            var subscriptionClient = CreateSubscriptionClient(topic, subscriptionName, onMessagesReceived, consumerId,
                                                              Configuration.Instance.GetBackOffIncrement(), fullLoadThreshold, waitInterval);
            _subscriptionClients.Add(subscriptionClient);
            return subscriptionClient;
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

        public void Dispose()
        {
            if (!_disposed)
            {
                _topicClients.Values.ForEach(client => client.Stop());
                _queueClients.Values.ForEach(client => client.Stop());
                _disposed = true;
            }
        }

        //private void StopQueueClients()
        //{
        //    _queueConsumers.ForEach(client => client.Stop());
        //}

        //private void StopSubscriptionClients()
        //{
        //    _subscriptionClients.ForEach(client => client.Stop());
        //}

        #region private methods

        private KafkaProducer GetTopicClient(string topic)
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

        private KafkaProducer GetQueueClient(string queue)
        {
            var queueClient = _queueClients.TryGetValue(queue);
            if (queueClient == null)
            {
                queueClient = CreateQueueClient(queue);
                _queueClients.GetOrAdd(queue, queueClient);
            }
            return queueClient;
        }

        private bool TopicExsits(string topic)
        {
            return true;
            //var managerConfig = new KafkaSimpleManagerConfiguration
            //{
            //    FetchSize = KafkaSimpleManagerConfiguration.DefaultFetchSize,
            //    BufferSize = KafkaSimpleManagerConfiguration.DefaultBufferSize,
            //    Zookeeper = _brokerList
            //};
            //using (var kafkaManager = new KafkaSimpleManager<string, Kafka.Client.Messages.Message>(managerConfig))
            //{
            //    try
            //    {
            //        // get all available partitions for a topic through the manager
            //        var allPartitions = kafkaManager.GetTopicPartitionsFromZK(topic);
            //        return allPartitions.Count > 0;
            //    }
            //    catch (Exception)
            //    {
            //        return false;
            //    }
            //}
        }

        public void CreateTopic(string topic)
        {
            //var producerConfiguration = new ProducerConfiguration(new List<BrokerConfiguration>())
            //{
            //    RequiredAcks = -1,
            //    TotalNumPartitions = 3,
            //    ZooKeeper = GetZooKeeperConfiguration(_brokerList)
            //};
            //while (true)
            //{
            //    using (var producer = new Producer(producerConfiguration))
            //    {
            //        try
            //        {
            //            var data = new ProducerData<string, Kafka.Client.Messages.Message>(topic, string.Empty, new Kafka.Client.Messages.Message(new byte[0]));
            //            producer.Send(data);
            //            break;
            //        }
            //        catch (Exception ex)
            //        {
            //            if (TopicExsits(topic))
            //            {
            //                break;
            //            }
            //            _logger.Error($"Create topic {topic} failed", ex);
            //            Task.Delay(200).Wait();
            //        }
            //    }
            //}
        }

        private void CreateTopicIfNotExists(string topic)
        {
            if (!TopicExsits(topic))
            {
                CreateTopic(topic);
            }
        }

        private KafkaConsumer CreateQueueConsumer(string queue,
                                                  OnMessagesReceived onMessagesReceived,
                                                  string consumerId = null,
                                                  int backOffIncrement = 30,
                                                  int fullLoadThreshold = 1000,
                                                  int waitInterval = 1000)
        {
            CreateTopicIfNotExists(queue);
            var queueConsumer = new KafkaConsumer(_brokerList, queue, $"{queue}.consumer", consumerId,
                                                  BuildOnKafkaMessageReceived(onMessagesReceived),
                                                  backOffIncrement, fullLoadThreshold, waitInterval);
            return queueConsumer;
        }

        private KafkaProducer CreateQueueClient(string queue)
        {
            CreateTopicIfNotExists(queue);
            var queueClient = new KafkaProducer(queue, _brokerList);
            return queueClient;
        }

        private KafkaProducer CreateTopicClient(string topic)
        {
            CreateTopicIfNotExists(topic);
            return new KafkaProducer(topic, _brokerList);
        }

        private KafkaConsumer CreateSubscriptionClient(string topic,
                                                       string subscriptionName,
                                                       OnMessagesReceived onMessagesReceived,
                                                       string consumerId = null,
                                                       int backOffIncrement = 30,
                                                       int fullLoadThreshold = 1000,
                                                       int waitInterval = 1000)
        {
            CreateTopicIfNotExists(topic);
            return new KafkaConsumer(_brokerList, topic, subscriptionName, consumerId,
                                     BuildOnKafkaMessageReceived(onMessagesReceived), backOffIncrement, fullLoadThreshold, waitInterval);
        }

        private OnKafkaMessageReceived BuildOnKafkaMessageReceived(OnMessagesReceived onMessagesReceived)
        {
            return (consumer, message) =>
            {
                var kafkaMessage = message.Value;
                var messageContext = new MessageContext(kafkaMessage, message.Partition, message.Offset);
                onMessagesReceived(messageContext);
            };
        }

        #endregion
    }
}