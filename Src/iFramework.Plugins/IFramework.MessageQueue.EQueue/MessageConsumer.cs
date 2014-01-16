using IFramework.Message;
using IFramework.Message.Impl;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Logging;
using EQueueClients = EQueue.Clients;
using EQueue.Clients.Producers;
using EQueue.Clients.Consumers;
using EQueue.Protocols;
using IFramework.MessageQueue.MessageFormat;

namespace IFramework.MessageQueue.EQueue
{
    public abstract class MessageConsumer<TMessage> : 
        IMessageConsumer, 
        EQueueClients.Consumers.IMessageHandler
    {
        public decimal MessageCount { get; protected set; }
        protected decimal HandledMessageCount { get; set; }
        protected Consumer Consumer { get; set; }
        protected readonly ILogger _Logger;

        public MessageConsumer()
        {
            _Logger = IoCFactory.Resolve<ILoggerFactory>().Create(this.GetType());
        }

        public MessageConsumer(string id, ConsumerSettings consumerSettings, string groupName, MessageModel messageModel, string subscribeTopic)
            : this()
        {
            Consumer = new Consumer(id, consumerSettings, groupName, MessageModel.Clustering, this)
                .Subscribe(subscribeTopic);
        }

        public virtual void Start()
        {
            try
            {
                //Consumer.Start();
            }
            catch (Exception e)
            {
                _Logger.Error(e.GetBaseException().Message, e);
            }

        }

        public virtual string GetStatus()
        {
            return string.Format("consumer handled message count: {0}<br>", HandledMessageCount);
        }

        public virtual void Handle(global::EQueue.Protocols.Message message)
        {
            ConsumeMessage(message.Body.GetMessage<TMessage>());
            HandledMessageCount++;
        }

        protected abstract void ConsumeMessage(TMessage messageContext);
    }
}
