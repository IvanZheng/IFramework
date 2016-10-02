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
using IFramework.Message.Impl;

namespace IFramework.MessageQueue.EQueue
{
    public class EQueueClient : IMessageQueueClient
    {
        public string ClusterName { get; set; }
        public List<System.Net.IPEndPoint> NameServerList { get; set; }
        protected List<EQueueConsumer> _subscriptionClients;
        protected List<EQueueConsumer> _queueConsumers;
        protected List<Task> _subscriptionClientTasks;
        protected List<Task> _commandClientTasks;
        protected ILogger _logger = null;

      
        public EQueueProducer _producer { get; protected set; }


        public EQueueClient(string clusterName, List<System.Net.IPEndPoint> nameServerList)
        {
            ClusterName = clusterName;
            NameServerList = nameServerList;
            _subscriptionClients = new List<EQueueConsumer>();
            _queueConsumers = new List<EQueueConsumer>();
            _subscriptionClientTasks = new List<Task>();
            _commandClientTasks = new List<Task>();
            _logger = IoCFactory.Resolve<ILoggerFactory>().Create(this.GetType().Name);
            _producer = new EQueueProducer(ClusterName, NameServerList);
            _producer.Start();
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
            return new EQueueMessages.Message(topic, 1, Encoding.UTF8.GetBytes(jsonValue));
        }

        public void Publish(IMessageContext messageContext, string topic)
        {
            _producer.Send(GetEQueueMessage(messageContext, topic), messageContext.Key);
        }

        public void Send(IMessageContext messageContext, string queue)
        {
            _producer.Send(GetEQueueMessage(messageContext, queue), messageContext.Key);
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
            if (sagaInfo != null)
            {
                messageContext.SagaInfo = sagaInfo;
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
                    messages = equeueConsumer.PullMessages(100, 2000, cancellationTokenSource.Token);
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
            var consumer = new EQueueConsumer(ClusterName, NameServerList, topic, subscriptionName, consumerId, fullLoadThreshold, waitInterval);
            consumer.Start();
            return consumer;
        }

        EQueueConsumer CreateQueueConsumer(string commandQueueName, string consumerId, int fullLoadThreshold, int waitInterval)
        {
            var consumer = new EQueueConsumer(ClusterName, NameServerList, commandQueueName, commandQueueName, consumerId, fullLoadThreshold, waitInterval);
            consumer.Start();
            return consumer;
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
