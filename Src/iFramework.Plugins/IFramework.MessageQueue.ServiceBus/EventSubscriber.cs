using System;
using System.Collections.Generic;
using System.Linq;
using IFramework.Message;
using System.Threading.Tasks;
using IFramework.Infrastructure;
using Microsoft.ServiceBus.Messaging;
using IFramework.MessageQueue.ServiceBus.MessageFormat;
using IFramework.Infrastructure.Unity.LifetimeManagers;
using IFramework.SysExceptions;
using IFramework.Command;
using System.Threading;

namespace IFramework.MessageQueue.ServiceBus
{
    public class EventSubscriber : MessageProcessor, IMessageConsumer
    {
        readonly IHandlerProvider _handlerProvider;
        readonly string[] _topics;
        readonly List<Task> _consumeWorkTasks;
        readonly string _subscriptionName;
        public EventSubscriber(string serviceBusConnectionString,
                               IHandlerProvider handlerProvider,
                               string subscriptionName,
                               params string[] topics)
            : base(serviceBusConnectionString)
        {
            _handlerProvider = handlerProvider;
            _topics = topics;
            _subscriptionName = subscriptionName;
            _consumeWorkTasks = new List<Task>();
        }

        public void Start()
        {
            _topics.ForEach(topic =>
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(topic))
                    {
                        // Receive messages
                        var subscriptionClient = CreateSubscriptionClient(topic, _subscriptionName);
                        _consumeWorkTasks.Add(Task.Factory.StartNew(ConsumeMessages, subscriptionClient, TaskCreationOptions.LongRunning));
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
            _consumeWorkTasks.ForEach(consumeWorkTask =>
            {
                var subscriptionClient = consumeWorkTask.AsyncState as SubscriptionClient;
                if (subscriptionClient != null)
                {
                    subscriptionClient.Close();
                }
                if (!consumeWorkTask.Wait(2000))
                {
                    _logger.ErrorFormat("receiver can't be stopped!");
                }
            });
        }

        protected virtual void ConsumeMessages(object stateObject)
        {
            var subscriptionClient = stateObject as SubscriptionClient;
            if (subscriptionClient == null)
            {
                return;
            }
            while (!_exit)
            {
                try
                {
                    BrokeredMessage brokeredMessage = null;
                    brokeredMessage = subscriptionClient.Receive();
                    if (brokeredMessage != null)
                    {
                        ConsumeMessage(brokeredMessage);
                        brokeredMessage.Complete();
                        MessageCount++;
                    }
                }
                catch (Exception ex)
                {
                    Thread.Sleep(1000);
                    _logger.Error(ex.GetBaseException().Message, ex);
                }
            }
        }


        protected void ConsumeMessage(BrokeredMessage brokeredMessage)
        {
            var eventContext = new MessageContext(brokeredMessage);
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
                    try
                    {
                        var messageHandler = IoCFactory.Resolve(messageHandlerType);
                        ((dynamic)messageHandler).Handle((dynamic)message);
                        var commandContexts = eventContext.ToBeSentMessageContexts;
                        messageStore.SaveEvent(eventContext, subscriptionName, commandContexts);
                        if (commandContexts.Count > 0)
                        {
                            ((CommandBus)IoCFactory.Resolve<ICommandBus>()).SendCommands(commandContexts.AsEnumerable());
                        }
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
                    finally
                    {
                        PerMessageContextLifetimeManager.CurrentMessageContext = null;
                    }
                }
            });
        }

        public string GetStatus()
        {
            return string.Format("Handled message count {0}", MessageCount);
        }

        public decimal MessageCount { get; set; }
    }
}
