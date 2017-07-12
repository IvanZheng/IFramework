using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IFramework.Config;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Logging;
using IFramework.IoC;
using IFramework.Message;
using IFramework.Message.Impl;
using IFramework.MessageQueue.EQueue.MessageFormat;
using EQueueMessages = EQueue.Protocols;

namespace IFramework.MessageQueue.EQueue
{
    public class EQueueClient : IMessageQueueClient
    {
        protected ILogger _logger;
        protected List<EQueueConsumer> _queueConsumers;
        protected List<EQueueConsumer> _subscriptionClients;


        public EQueueClient(string clusterName, List<IPEndPoint> nameServerList)
        {
            ClusterName = clusterName;
            NameServerList = nameServerList;
            _subscriptionClients = new List<EQueueConsumer>();
            _queueConsumers = new List<EQueueConsumer>();
            _logger = IoCFactory.Resolve<ILoggerFactory>().Create(GetType().Name);
            _producer = new EQueueProducer(ClusterName, NameServerList);
            _producer.Start();
        }

        public string ClusterName { get; set; }
        public List<IPEndPoint> NameServerList { get; set; }


        public EQueueProducer _producer { get; protected set; }

        public void Dispose()
        {
            _producer.Stop();
        }

        public Task PublishAsync(IMessageContext messageContext, string topic, CancellationToken cancellationToken)
        {
            return _producer.SendAsync(GetEQueueMessage(messageContext, topic), messageContext.Key, cancellationToken);
        }

        public Task SendAsync(IMessageContext messageContext, string queue, CancellationToken cancellationToken)
        {
            return _producer.SendAsync(GetEQueueMessage(messageContext, queue), messageContext.Key, cancellationToken);
        }

        public ICommitOffsetable StartQueueClient(string commandQueueName,
                                                  string consumerId,
                                                  OnMessagesReceived onMessagesReceived,
                                                  ConsumerConfig consumerConfig = null)
        {
            commandQueueName = Configuration.Instance.FormatMessageQueueName(commandQueueName);
            consumerId = Configuration.Instance.FormatMessageQueueName(consumerId);
            var queueConsumer = CreateQueueConsumer(commandQueueName, consumerId, onMessagesReceived, consumerConfig);
            _queueConsumers.Add(queueConsumer);
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
            var subscriptionClient = CreateSubscriptionClient(topic, subscriptionName, onMessagesReceived,
                                                              consumerId, consumerConfig);
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
            if (sagaInfo != null)
            {
                messageContext.SagaInfo = sagaInfo;
            }
            return messageContext;
        }

        protected EQueueMessages.Message GetEQueueMessage(IMessageContext messageContext, string topic)
        {
            topic = Configuration.Instance.FormatMessageQueueName(topic);
            var jsonValue = ((MessageContext) messageContext).EqueueMessage.ToJson();
            return new EQueueMessages.Message(topic, 1, Encoding.UTF8.GetBytes(jsonValue));
        }

        #region private methods

        private OnEQueueMessageReceived BuildOnEQueueMessageReceived(OnMessagesReceived onMessagesReceived)
        {
            return (consumer, message) =>
            {
                var equeueMessage = Encoding.UTF8.GetString(message.Body).ToJsonObject<EQueueMessage>();
                var messageContext = new MessageContext(equeueMessage, message.QueueId, message.QueueOffset);
                onMessagesReceived(messageContext);
            };
        }


        private EQueueConsumer CreateSubscriptionClient(string topic,
                                                        string subscriptionName,
                                                        OnMessagesReceived onMessagesReceived,
                                                        string consumerId = null,
                                                        ConsumerConfig consumerConfig = null)
        {
            var consumer = new EQueueConsumer(ClusterName, NameServerList, topic, subscriptionName, consumerId,
                                              BuildOnEQueueMessageReceived(onMessagesReceived),
                                              consumerConfig);
            return consumer;
        }

        private EQueueConsumer CreateQueueConsumer(string commandQueueName,
                                                   string consumerId,
                                                   OnMessagesReceived onMessagesReceived,
                                                   ConsumerConfig consumerConfig = null)
        {
            var consumer = new EQueueConsumer(ClusterName, NameServerList, commandQueueName,
                                              commandQueueName, consumerId,
                                              BuildOnEQueueMessageReceived(onMessagesReceived),
                                              consumerConfig);
            return consumer;
        }

        #endregion
    }
}