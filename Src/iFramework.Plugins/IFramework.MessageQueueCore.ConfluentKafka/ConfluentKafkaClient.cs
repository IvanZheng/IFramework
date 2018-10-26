using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka.Serialization;
using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.Infrastructure;
using IFramework.Message;
using IFramework.Message.Impl;
using IFramework.MessageQueue;
using IFramework.MessageQueueCore.ConfluentKafka.MessageFormat;
using Microsoft.Extensions.Logging;

namespace IFramework.MessageQueueCore.ConfluentKafka
{
    public class ConfluentKafkaClient : IMessageQueueClient
    {
        protected string BrokerList;
        protected bool Disposed;
        protected ILogger Logger;
        protected ConcurrentDictionary<string, KafkaProducer<string, KafkaMessage>> QueueClients;
        protected List<KafkaConsumer<string, KafkaMessage>> QueueConsumers;
        protected List<KafkaConsumer<string, KafkaMessage>> SubscriptionClients;
        protected ConcurrentDictionary<string, KafkaProducer<string, KafkaMessage>> TopicClients;


        public ConfluentKafkaClient(string brokerList)
        {
            BrokerList = brokerList;
            QueueClients = new ConcurrentDictionary<string, KafkaProducer<string, KafkaMessage>>();
            TopicClients = new ConcurrentDictionary<string, KafkaProducer<string, KafkaMessage>>();
            SubscriptionClients = new List<KafkaConsumer<string, KafkaMessage>>();
            QueueConsumers = new List<KafkaConsumer<string, KafkaMessage>>();
            Logger = IoCFactory.GetService<ILoggerFactory>().CreateLogger(GetType().Name);
        }


        //public void CompleteMessage(IMessageContext messageContext)
        //{
        //    (messageContext as MessageContext).Complete();
        //    _logger.Debug($"complete message {messageContext.Message.ToJson()}");
        //}


        public Task PublishAsync(IMessageContext messageContext, string topic, CancellationToken cancellationToken)
        {
            topic = Configuration.Instance.FormatMessageQueueName(topic);
            var topicClient = GetTopicClient(topic);
            var message = ((MessageContext)messageContext).KafkaMessage;
            return topicClient.SendAsync(messageContext.Key, message, cancellationToken);
        }

        public Task SendAsync(IMessageContext messageContext, string queue, CancellationToken cancellationToken)
        {
            queue = Configuration.Instance.FormatMessageQueueName(queue);
            var queueClient = GetQueueClient(queue);

            var message = ((MessageContext)messageContext).KafkaMessage;
            return queueClient.SendAsync(messageContext.Key, message, cancellationToken);
        }

        public ICommitOffsetable StartQueueClient(string commandQueueName,
                                                  string consumerId,
                                                  OnMessagesReceived onMessagesReceived,
                                                  ConsumerConfig consumerConfig = null)
        {
            commandQueueName = Configuration.Instance.FormatMessageQueueName(commandQueueName);
            consumerId = Configuration.Instance.FormatMessageQueueName(consumerId);
            var queueConsumer = CreateQueueConsumer(commandQueueName, onMessagesReceived, consumerId,
                                                    consumerConfig);
            QueueConsumers.Add(queueConsumer);
            return queueConsumer;
        }

        public ICommitOffsetable StartSubscriptionClient(string topic,
                                                         string subscriptionName,
                                                         string consumerId,
                                                         OnMessagesReceived onMessagesReceived,
                                                         ConsumerConfig consumerConfig = null)
        {
            topic = Configuration.Instance.FormatMessageQueueName(topic);
            subscriptionName = Configuration.Instance.FormatMessageQueueName(subscriptionName);
            var subscriptionClient = CreateSubscriptionClient(topic, subscriptionName, onMessagesReceived, consumerId,
                                                              consumerConfig);
            SubscriptionClients.Add(subscriptionClient);
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
            messageContext.Ip = Utility.GetLocalIPV4()?.ToString();
            if (!string.IsNullOrEmpty(correlationId))
            {
                messageContext.CorrelationId = correlationId;
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
            if (!Disposed)
            {
                TopicClients.Values.ForEach(client => client.Stop());
                QueueClients.Values.ForEach(client => client.Stop());
                Disposed = true;
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

        private KafkaProducer<string, KafkaMessage> GetTopicClient(string topic)
        {
            KafkaProducer<string, KafkaMessage> topicClient = null;
            TopicClients.TryGetValue(topic, out topicClient);
            if (topicClient == null)
            {
                topicClient = CreateTopicClient(topic);
                TopicClients.GetOrAdd(topic, topicClient);
            }
            return topicClient;
        }

        private KafkaProducer<string, KafkaMessage> GetQueueClient(string queue)
        {
            var queueClient = QueueClients.TryGetValue(queue);
            if (queueClient == null)
            {
                queueClient = CreateQueueClient(queue);
                QueueClients.GetOrAdd(queue, queueClient);
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

        private KafkaConsumer<string, KafkaMessage> CreateQueueConsumer(string queue,
                                                                        OnMessagesReceived onMessagesReceived,
                                                                        string consumerId = null,
                                                                        ConsumerConfig consumerConfig = null)
        {
            CreateTopicIfNotExists(queue);
            var queueConsumer = new KafkaConsumer<string, KafkaMessage>(BrokerList, queue, $"{queue}.consumer", consumerId,
                                                                        BuildOnKafkaMessageReceived(onMessagesReceived),
                                                                        new StringDeserializer(Encoding.UTF8),
                                                                        new KafkaMessageDeserializer(),
                                                                        consumerConfig);
            return queueConsumer;
        }

        private KafkaProducer<string, KafkaMessage> CreateQueueClient(string queue)
        {
            CreateTopicIfNotExists(queue);
            var queueClient = new KafkaProducer<string, KafkaMessage>(queue, BrokerList, new StringSerializer(Encoding.UTF8), new KafkaMessageSerializer());
            return queueClient;
        }

        private KafkaProducer<string, KafkaMessage> CreateTopicClient(string topic)
        {
            CreateTopicIfNotExists(topic);
            return new KafkaProducer<string, KafkaMessage>(topic, BrokerList, new StringSerializer(Encoding.UTF8), new KafkaMessageSerializer());
        }

        private KafkaConsumer<string, KafkaMessage> CreateSubscriptionClient(string topic,
                                                                             string subscriptionName,
                                                                             OnMessagesReceived onMessagesReceived,
                                                                             string consumerId = null,
                                                                             ConsumerConfig consumerConfig = null)
        {
            CreateTopicIfNotExists(topic);
            return new KafkaConsumer<string, KafkaMessage>(BrokerList, topic, subscriptionName, consumerId,
                                                           BuildOnKafkaMessageReceived(onMessagesReceived),
                                                           new StringDeserializer(Encoding.UTF8),
                                                           new KafkaMessageDeserializer(),
                                                           consumerConfig);
        }

        private OnKafkaMessageReceived<string, KafkaMessage> BuildOnKafkaMessageReceived(OnMessagesReceived onMessagesReceived)
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