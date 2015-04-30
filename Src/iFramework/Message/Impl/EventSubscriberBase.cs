using IFramework.Command;
using IFramework.Event;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Logging;
using IFramework.Infrastructure.Unity.LifetimeManagers;
using IFramework.SysExceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;

namespace IFramework.Message.Impl
{
    public abstract class EventSubscriberBase
    {
        protected IHandlerProvider _handlerProvider;
        protected string _subscriptionName;
        protected ILogger _logger;

        protected abstract IMessageContext NewMessageContext(IMessage message);

        public EventSubscriberBase(IHandlerProvider handlerProvider, string subscriptionName)
        {
            _handlerProvider = handlerProvider;
            _subscriptionName = subscriptionName;
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
                            var eventBus = IoCFactory.Resolve<IEventBus>();
                            eventBus.GetMessages().ForEach(msg => messageContexts.Add(NewMessageContext(msg)));

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
                        messageStore.SaveFailHandledEvent(eventContext, subscriptionName, e);
                    }
                    if (success)
                    {
                        if (commandContexts.Count > 0)
                        {
                            IoCFactory.Resolve<ICommandBus>().Send(commandContexts.AsEnumerable());
                        }
                        if (messageContexts.Count > 0)
                        {
                            IoCFactory.Resolve<IEventPublisher>().Publish(messageContexts.ToArray());
                        }
                    }
                }
                PerMessageContextLifetimeManager.CurrentMessageContext = null;
            });
        }
    }
}
