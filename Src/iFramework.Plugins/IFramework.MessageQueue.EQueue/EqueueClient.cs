using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EQueueMessages = EQueue.Protocols;
using IFramework.Message;
using IFramework.MessageQueue.EQueue.MessageFormat;
using IFramework.Config;
using IFramework.Infrastructure;
using System.Threading;
using IFramework.Infrastructure.Logging;
using IFramework.IoC;

namespace IFramework.MessageQueue.EQueue
{
    public class EqueueClient : IMessageQueueClient
    {
        public string BrokerAddress { get; protected set; }
        public int ProducerPort { get; protected set; }
        public int ConsumerPort { get; protected set; }
        public int AdminPort { get; protected set; }
        protected List<EQueueConsumer> _subscriptionClients;
        protected List<EQueueConsumer> _queueConsumers;
        protected List<Task> _subscriptionClientTasks;
        protected List<Task> _commandClientTasks;
        protected ILogger _logger = null;

        EQueueProducer _producer;
        public EQueueProducer Producer
        {
            get
            {
                return _producer ?? (_producer = new EQueueProducer(BrokerAddress, ProducerPort, AdminPort));
            }
        }


        public EqueueClient(string brokerAddress, int producerPort = 5000, int consumerPort = 5001, int adminPort = 5002)
        {
            BrokerAddress = brokerAddress;
            ProducerPort = producerPort;
            ConsumerPort = consumerPort;
            AdminPort = adminPort;
            _subscriptionClients = new List<EQueueConsumer>();
            _queueConsumers = new List<EQueueConsumer>();
            _subscriptionClientTasks = new List<Task>();
            _commandClientTasks = new List<Task>();
            _logger = IoCFactory.Resolve<ILoggerFactory>().Create(this.GetType().Name);
        }

        public void Dispose()
        {
            StopQueueClients();
            StopSubscriptionClients();
        }

        protected EQueueMessages.Message GetEQueueMessage(IMessageContext messageContext, string topic)
        {
            topic = Configuration.Instance.FormatMessageQueueName(topic);
            var jsonValue = ((MessageContext)messageContext).EqueueMessage.ToJson();
            return new EQueueMessages.Message(topic, 0, Encoding.UTF8.GetBytes(jsonValue));
        }

        public void Publish(IMessageContext messageContext, string topic)
        {
            Producer.Send(GetEQueueMessage(messageContext, topic), messageContext.Key);
        }

        public void Send(IMessageContext messageContext, string queue)
        {
            Producer.Send(GetEQueueMessage(messageContext, queue), messageContext.Key);
        }

        public Action<IMessageContext> StartQueueClient(string commandQueueName, string consumerId, OnMessagesReceived onMessagesReceived, int fullLoadThreshold = 1000, int waitInterval = 1000)
        {
            commandQueueName = Configuration.Instance.FormatMessageQueueName(commandQueueName);
            consumerId = Configuration.Instance.FormatMessageQueueName(consumerId);
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

        void ReceiveMessages(CancellationTokenSource cancellationTokenSource, OnMessagesReceived onMessagesReceived, EQueueConsumer equeueConsumer)
        {
            IEnumerable<EQueueMessages.QueueMessage> messages = null;

            #region peek messages that not been consumed since last time
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    messages = equeueConsumer.GetMessages(cancellationTokenSource.Token);
                    foreach (var message in messages)
                    {
                        try
                        {
                            var equeueMessage = Encoding.UTF8.GetString(message.Body).ToJsonObject<EQueueMessage>();
                            var messageContext = new MessageContext(equeueMessage, message.QueueId, message.QueueOffset);
                            equeueConsumer.AddMessage(message);
                            onMessagesReceived(messageContext);
                            equeueConsumer.BlockIfFullLoad();
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
                            if (message.Body != null)
                            {
                                equeueConsumer.RemoveMessage(message.QueueId, message.QueueOffset);
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

        #region private methods
        EQueueConsumer CreateSubscriptionClient(string topic, string subscriptionName, string consumerId = null, int fullLoadThreshold = 1000, int waitInterval = 1000)
        {
            return new EQueueConsumer(BrokerAddress, ConsumerPort, topic, subscriptionName, consumerId, fullLoadThreshold, waitInterval);
        }

        EQueueConsumer CreateQueueConsumer(string commandQueueName, string consumerId, int fullLoadThreshold, int waitInterval)
        {
            return new EQueueConsumer(BrokerAddress, ConsumerPort, commandQueueName, commandQueueName, consumerId, fullLoadThreshold, waitInterval);
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
        #endregion
    }
}
