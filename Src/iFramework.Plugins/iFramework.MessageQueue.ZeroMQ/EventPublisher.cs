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

namespace IFramework.MessageQueue.ZeroMQ
{
    class EventPublisher : IEventPublisher
    {
        protected ZmqSocket ZmqEventPublisher { get; set; }
        protected BlockingCollection<object> MessageQueue { get; set; } 

        public EventPublisher(string pubEndPoint)
        {
            MessageQueue = new BlockingCollection<object>();
            if (!string.IsNullOrWhiteSpace(pubEndPoint))
            {
                ZmqEventPublisher = ZeroMessageQueue.ZmqContext.CreateSocket(SocketType.PUB);
                ZmqEventPublisher.Bind(pubEndPoint);
                Task.Factory.StartNew(PublishEvent);
            }
        }

        public void Publish(IEnumerable<object> events)
        { 
            events.ForEach(@event =>
            {
                Publish(@event);
            });
        }

        public void Publish(object @event)
        {
            if (ZmqEventPublisher != null)
            {
                MessageQueue.Add(@event);
            }
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
