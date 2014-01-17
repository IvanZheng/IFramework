using IFramework.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFramework.Infrastructure;
using ZeroMQ;
using IFramework.Message.Impl;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using IFramework.Message;
using IFramework.MessageQueue.MessageFormat;

namespace IFramework.MessageQueue.ZeroMQ
{
    class EventPublisher : IEventPublisher
    {
        protected ZmqSocket ZmqEventPublisher { get; set; }
        protected BlockingCollection<IMessageContext> MessageQueue { get; set; }

        public EventPublisher(string pubEndPoint)
        {
            MessageQueue = new BlockingCollection<IMessageContext>();
            if (!string.IsNullOrWhiteSpace(pubEndPoint))
            {
                ZmqEventPublisher = ZeroMessageQueue.ZmqContext.CreateSocket(SocketType.PUB);
                ZmqEventPublisher.Bind(pubEndPoint);
                Task.Factory.StartNew(PublishEvent);
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
            IMessageContext messageContext = null;
            if (ZmqEventPublisher != null)
            {
                messageContext = new MessageContext(@event);
                MessageQueue.Add(messageContext);
            }
            return messageContext;
        }

        void PublishEvent()
        {
            while (true)
            {
                var eventContext = MessageQueue.Take();
                ZmqEventPublisher.Send(eventContext.ToJson(), Encoding.UTF8);
            }
        }
    }
}
