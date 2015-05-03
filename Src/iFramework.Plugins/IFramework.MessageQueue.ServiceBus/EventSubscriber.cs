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
using IFramework.Event;
using System.Transactions;
using IFramework.Infrastructure.Logging;

namespace IFramework.MessageQueue.ServiceBus
{
    public class EventSubscriber : IFramework.Message.Impl.EventSubscriberBase, IMessageConsumer
    {
        volatile bool _exit = false;
        readonly string[] _topics;
        readonly List<Task> _consumeWorkTasks;
        protected ServiceBusClient _serviceBusClient;
        public EventSubscriber(string serviceBusConnectionString,
                               IHandlerProvider handlerProvider,
                               string subscriptionName,
                               params string[] topics)
            : base(handlerProvider, subscriptionName)
        {
            _serviceBusClient = new ServiceBusClient(serviceBusConnectionString);
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
                        var subscriptionClient = _serviceBusClient.CreateSubscriptionClient(topic, _subscriptionName);
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
                        var eventContext = new MessageContext(brokeredMessage);
                        ConsumeMessage(eventContext);
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

        public string GetStatus()
        {
            return string.Format("Handled message count {0}", MessageCount);
        }

        public decimal MessageCount { get; set; }

        protected override IMessageContext NewMessageContext(IMessage message)
        {
            return new MessageContext(message);
        }
    }
}
