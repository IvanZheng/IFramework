using IFramework.Event;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Logging;
using IFramework.Message;
using IFramework.MessageQueue.MessageFormat;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IFramework.MessageQueue.ServiceBus
{
    public class EventPublisher : MessageProcessor, IEventPublisher
    {
        protected BlockingCollection<IMessageContext> MessageQueue { get; set; }
        protected string _topic;
        protected Task _WorkTask;
        protected TopicClient _topicClient;

        public EventPublisher(string serviceBusConnectioniString, string topic)
            : base(serviceBusConnectioniString)
        {
            MessageQueue = new BlockingCollection<IMessageContext>();
            _topic = topic;

        }

        public void Start()
        {
            if (!string.IsNullOrWhiteSpace(_topic))
            {
                using (var messageStore = IoCFactory.Resolve<IMessageStore>())
                {
                    messageStore.GetAllUnPublishedEvents()
                        .ForEach(eventContext => MessageQueue.Add(eventContext));
                }
                _topicClient = CreateTopicClient(_topic);
                _WorkTask = Task.Factory.StartNew(PublishEvent, TaskCreationOptions.LongRunning);
            }
        }

        public void Stop()
        {
            if (_WorkTask != null)
            {
                MessageQueue.CompleteAdding();
                if (_WorkTask.Wait(2000))
                {
                    _topicClient.Close();
                    _WorkTask.Dispose();
                }
                else
                {
                    _logger.ErrorFormat("consumer can't be stopped!");
                }
            }
        }

        public void Publish(params IEvent[] events)
        {
            events.ForEach(@event => MessageQueue.Add(new MessageContext(@event)));
        }

        public void Publish(params IMessageContext[] eventContexts)
        {
            eventContexts.ForEach(@messageContext => MessageQueue.Add(messageContext));
        }

        void PublishEvent()
        {
            try
            {
                while (true)
                {
                    var eventContext = MessageQueue.Take();
                    while(true)
                    {
                        try
                        {
                            _topicClient.Send(((MessageContext)eventContext).BrokeredMessage);
                            using (var messageStore = IoCFactory.Resolve<IMessageStore>())
                            {
                                messageStore.RemovePublishedEvent(eventContext.MessageID);
                            }
                            break;
                        }
                        catch (Exception)
                        {
                            Thread.Sleep(1000);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Debug("end publish working", ex);
            }
        }
    }
}
