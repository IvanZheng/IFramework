using IFramework.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFramework.Infrastructure;
using IFramework.Message.Impl;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using IFramework.Infrastructure.Logging;
using IFramework.Message;
using IFramework.MessageQueue.EQueue.MessageFormat;
using EQueueClientsProducers = EQueue.Clients.Producers;
using EQueueProtocols = EQueue.Protocols;

namespace IFramework.MessageQueue.EQueue
{
    public class EventPublisher : IEventPublisher
    {
        protected ILogger _Logger;
        protected BlockingCollection<EQueueProtocols.Message> MessageQueue { get; set; }
        protected EQueueClientsProducers.Producer Producer { get; set; }
        protected string Id { get; set; }
        protected string Topic { get; set; }

        public EventPublisher(string id, string topic, EQueueClientsProducers.ProducerSetting producerSetting)
        {
            Topic = topic;
            Id = id;
            _Logger = IoCFactory.Resolve<ILoggerFactory>().Create(this.GetType());
            MessageQueue = new BlockingCollection<EQueueProtocols.Message>();
            Producer = new EQueueClientsProducers.Producer(Id, producerSetting);
        }

        public void Start()
        {
            Producer.Start();
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    try
                    {
                        var message = MessageQueue.Take();
                        var sendResult = Producer.Send(message, string.Empty);
                        if (sendResult.SendStatus == EQueueClientsProducers.SendStatus.Success)
                        {
                            _Logger.DebugFormat("publish event success, body {0}", message.ToJson());
                        }
                        else
                        {
                            _Logger.ErrorFormat("Send event failed {0}", sendResult.SendStatus.ToString());
                        }
                    }
                    catch (Exception ex)
                    {
                        _Logger.Error(ex.GetBaseException().Message, ex);
                    }
                }
            });
        }

        public void Stop()
        {
            Producer.Shutdown();
        }

        public void Publish(params IEvent[] events)
        {
            List<IMessageContext> messageContexts = new List<IMessageContext>();
            events.ForEach(@event =>
            {
                var message = new MessageContext(Topic, @event).EQueueMessage;
                MessageQueue.Add(message);
            });
        }

        public void Publish(params IMessageContext[] eventContexts)
        {
            eventContexts.ForEach(eventContext => MessageQueue.Add((eventContext as MessageContext).EQueueMessage));
        }
    }
}
