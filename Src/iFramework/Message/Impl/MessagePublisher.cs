using IFramework.Infrastructure;
using IFramework.Infrastructure.Logging;
using IFramework.Message;
using IFramework.MessageQueue;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IFramework.Message.Impl
{
    public class MessagePublisher : IMessagePublisher
    {
        protected BlockingCollection<IMessageContext> _MessageQueue { get; set; }
        protected string _DefaultTopic;
        protected Task _WorkTask;
        protected IMessageQueueClient _MessageQueueClient;
        ILogger _Logger;

        public MessagePublisher(IMessageQueueClient messageQueueClient, string defaultTopic)
        {
            if (string.IsNullOrEmpty(defaultTopic))
            {
                throw new Exception("event publisher must have a default topic.");
            }
            _MessageQueueClient = messageQueueClient;
            _DefaultTopic = defaultTopic;
            _MessageQueue = new BlockingCollection<IMessageContext>();
            _Logger = IoCFactory.Resolve<ILoggerFactory>().Create(this.GetType());
        }

        public virtual void Start()
        {
            if (!string.IsNullOrWhiteSpace(_DefaultTopic))
            {
                using (var messageStore = IoCFactory.Resolve<IMessageStore>())
                {
                    messageStore.GetAllUnPublishedEvents()
                        .ForEach(eventContext => _MessageQueue.Add(eventContext));
                }
                _WorkTask = Task.Factory.StartNew(PublishEvent, TaskCreationOptions.LongRunning);
            }
        }

        public void Stop()
        {
            if (_WorkTask != null)
            {
                _MessageQueue.CompleteAdding();
                if (_WorkTask.Wait(2000))
                {
                    _MessageQueueClient.CloseTopicClients();
                    _WorkTask.Dispose();
                }
                else
                {
                    _Logger.ErrorFormat("consumer can't be stopped!");
                }
            }
        }

        public void Publish(params IMessage[] messages)
        {
            messages.ForEach(message => _MessageQueue.Add(_MessageQueueClient.WrapMessage(message)));
        }

        public void Publish(params Message.IMessageContext[] messageContexts)
        {
            messageContexts.ForEach(@messageContext => _MessageQueue.Add(messageContext));
        }

        void PublishEvent()
        {
            try
            {
                while (true)
                {
                    var eventContext = _MessageQueue.Take();
                    while (true)
                    {
                        try
                        {
                            _MessageQueueClient.Publish(eventContext, eventContext.Topic ?? _DefaultTopic);
                            //var topicClient = _serviceBusClient.GetTopicClient(eventContext.Topic ?? _topic);
                            //topicClient.Send(((MessageContext)eventContext).BrokeredMessage);
                            Task.Factory.StartNew(() =>
                            {
                                using (var messageStore = IoCFactory.Resolve<IMessageStore>())
                                {
                                    messageStore.RemovePublishedEvent(eventContext.MessageID);
                                }
                            });
                            break;
                        }
                        catch (Exception)
                        {
                            //if (ex is InvalidOperationException)
                            //{
                            //    eventContext = new MessageContext(eventContext.Message as IMessage);
                            //}
                            Thread.Sleep(1000);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _Logger.Debug("end publish working", ex);
            }
        }
    }
}
