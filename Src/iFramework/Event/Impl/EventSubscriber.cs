using IFramework.Message;
using IFramework.MessageQueue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Logging;
using IFramework.Infrastructure.Unity.LifetimeManagers;
using System.Transactions;
using IFramework.SysExceptions;
using IFramework.Command;

namespace IFramework.Event.Impl
{
    public class EventSubscriber : IMessageConsumer
    {
        readonly string[] _topics;
        protected IMessageQueueClient _MessageQueueClient;
        protected ICommandBus _commandBus;
        protected IMessagePublisher _messagePublisher;
        protected IHandlerProvider _handlerProvider;
        protected string _subscriptionName;
        protected ILogger _logger;
        public EventSubscriber(IMessageQueueClient messageQueueClient,
                               IHandlerProvider handlerProvider,
                               ICommandBus commandBus,
                               IMessagePublisher messagePublisher,
                               string subscriptionName,
                               params string[] topics)
        {
            _MessageQueueClient = messageQueueClient;
            _handlerProvider = handlerProvider;
            _topics = topics;
            _subscriptionName = subscriptionName;
            _messagePublisher = messagePublisher;
            _commandBus = commandBus;
            _logger = IoCFactory.Resolve<ILoggerFactory>().Create(this.GetType());
        }

        protected void ConsumeMessage(IMessageContext eventContext)
        {
            var message = eventContext.Message;
            var messageHandlerTypes = _handlerProvider.GetHandlerTypes(message.GetType());

            if (messageHandlerTypes.Count == 0)
            {
                return;
            }

            messageHandlerTypes.ForEach(messageHandlerType =>
            {
                PerMessageContextLifetimeManager.CurrentMessageContext = eventContext;
                eventContext.ToBeSentMessageContexts.Clear();
                var messageStore = IoCFactory.Resolve<IMessageStore>();
                var subscriptionName = string.Format("{0}.{1}", _subscriptionName, messageHandlerType.FullName);
                if (!messageStore.HasEventHandled(eventContext.MessageID, subscriptionName))
                {
                    bool success = false;
                    var messageContexts = new List<IMessageContext>();
                    List<IMessageContext> commandContexts = null;
                    var eventBus = IoCFactory.Resolve<IEventBus>();

                    try
                    {
                        var messageHandler = IoCFactory.Resolve(messageHandlerType);
                        using (var transactionScope = new TransactionScope(TransactionScopeOption.Required,
                                                           new TransactionOptions { IsolationLevel = System.Transactions.IsolationLevel.ReadUncommitted }))
                        {
                            ((dynamic)messageHandler).Handle((dynamic)message);

                            //get commands to be sent
                            commandContexts = eventContext.ToBeSentMessageContexts;
                            //get events to be published
                            eventBus.GetMessages().ForEach(msg => messageContexts.Add(_MessageQueueClient.WrapMessage(msg)));

                            messageStore.SaveEvent(eventContext, subscriptionName, commandContexts, messageContexts);
                            transactionScope.Complete();
                        }
                        success = true;
                    }
                    catch (Exception e)
                    {
                        if (e is DomainException)
                        {
                            _logger.Warn(message.ToJson(), e);
                        }
                        else
                        {
                            //IO error or sytem Crash
                            _logger.Error(message.ToJson(), e);
                        }
                        messageStore.Rollback();
                        eventBus.GetToPublishAnywayMessages().ForEach(msg => messageContexts.Add(_MessageQueueClient.WrapMessage(msg)));

                        messageStore.SaveFailHandledEvent(eventContext, subscriptionName, e, messageContexts.ToArray());
                    }
                    if (success)
                    {
                        if (commandContexts.Count > 0)
                        {
                            _commandBus.Send(commandContexts.AsEnumerable());
                        }
                        if (messageContexts.Count > 0)
                        {
                            _messagePublisher.Send(messageContexts.ToArray());
                        }
                    }
                }
                PerMessageContextLifetimeManager.CurrentMessageContext = null;
            });
        }
        public void Start()
        {
            _topics.ForEach(topic =>
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(topic))
                    {
                        _MessageQueueClient.StartSubscriptionClient(topic, _subscriptionName, OnMessageReceived);
                    }
                }
                catch (Exception e)
                {
                    _logger.Error(e.GetBaseException().Message, e);
                }
            });
        }

        public void Stop()
        {
            _MessageQueueClient.StopSubscriptionClients();
        }

        protected void OnMessageReceived(IMessageContext messageContext)
        {
            ConsumeMessage(messageContext);
            MessageCount++;
        }

        public string GetStatus()
        {
            return string.Format("Handled message count {0}", MessageCount);
        }

        public decimal MessageCount { get; set; }
    }
}
