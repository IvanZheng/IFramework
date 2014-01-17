using IFramework.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFramework.Infrastructure;
using IFramework.Message.Impl;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using EQueue.Clients.Producers;
using IFramework.Infrastructure.Logging;
using IFramework.Message;
using IFramework.MessageQueue.MessageFormat;

namespace IFramework.MessageQueue.EQueue
{
    public class EventPublisher : IEventPublisher
    {
        protected ILogger _Logger;
        protected BlockingCollection<IMessageContext> MessageQueue { get; set; }
        protected Producer Producer { get; set; }
        protected string Topic { get; set; }

        public EventPublisher(string topic, string brokerAddress, int brokerPort)
        {
            Topic = topic;
            _Logger = IoCFactory.Resolve<ILoggerFactory>().Create(this.GetType());
            MessageQueue = new BlockingCollection<IMessageContext>();
            try
            {
                Producer = new Producer(brokerAddress, brokerPort);
                Producer.Start();
                Task.Factory.StartNew(PublishEvent);
            }
            catch (Exception ex)
            {
                _Logger.Error(ex.GetBaseException().Message, ex);
            }
        }

        public IEnumerable<IMessageContext> Publish(IEnumerable<IDomainEvent> events)
        {
            List<IMessageContext> messageContexts = new List<IMessageContext>();
            events.ForEach(@event =>
            {
                messageContexts.Add(Publish(@event));
            });
            return messageContexts;
        }

        public IMessageContext Publish(IDomainEvent @event)
        {
            var messageContext = new MessageContext(@event);
            MessageQueue.Add(messageContext);
            return messageContext;
        }

        void PublishEvent()
        {
            while (true)
            {
                var eventContext = MessageQueue.Take();
                var messageBody = eventContext.GetMessageBytes();
                Producer.SendAsync(new global::EQueue.Protocols.Message(Topic, messageBody), string.Empty)
                        .ContinueWith(task =>
                          {
                              if (task.Result.SendStatus == SendStatus.Success)
                              {
                                  _Logger.DebugFormat("sent event {0}, body: {1}", eventContext.MessageID,
                                                                                  eventContext.Message.ToJson());
                              }
                              else
                              {
                                  _Logger.ErrorFormat("Send event {0}", task.Result.SendStatus.ToString());
                              }
                          });
            }
        }
    }
}
