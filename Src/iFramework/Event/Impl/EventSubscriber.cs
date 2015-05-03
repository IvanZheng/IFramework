using IFramework.Message;
using IFramework.MessageQueue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IFramework.Infrastructure;

namespace IFramework.Event.Impl
{
    public class EventSubscriber : IFramework.Message.Impl.EventSubscriberBase, IMessageConsumer
    {
        readonly string[] _topics;
        protected IMessageQueueClient _MessageQueueClient;

        public EventSubscriber(IMessageQueueClient messageQueueClient,
                               IHandlerProvider handlerProvider,
                               string subscriptionName,
                               params string[] topics)
            : base(handlerProvider, subscriptionName)
        {
            _MessageQueueClient = messageQueueClient;
            _handlerProvider = handlerProvider;
            _topics = topics;
            _subscriptionName = subscriptionName;
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

        protected override IMessageContext NewMessageContext(IMessage message)
        {
            return _MessageQueueClient.WrapMessage(message);
        }
    }
}
