using IFramework.Infrastructure;
using IFramework.Infrastructure.Logging;
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
    public abstract class MessageSender : IMessageSender
    {
        protected BlockingCollection<IMessageContext> _messageQueue { get; set; }
        protected string _defaultTopic;
        protected Task _sendMessageTask;
        protected IMessageQueueClient _messageQueueClient;
        protected ILogger _logger;

        public MessageSender(IMessageQueueClient messageQueueClient, string defaultTopic = null)
        {
            _messageQueueClient = messageQueueClient;
            _defaultTopic = defaultTopic;
            _messageQueue = new BlockingCollection<IMessageContext>();
            _logger = IoCFactory.Resolve<ILoggerFactory>().Create(this.GetType());
        }

        protected abstract IEnumerable<IMessageContext> GetAllUnSentMessages();
        protected abstract void Send(IMessageContext messageContext, string topic);
        protected abstract void CompleteSendingMessage(string messageId);

        public virtual void Start()
        {
            GetAllUnSentMessages().ForEach(eventContext => _messageQueue.Add(eventContext));
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            _sendMessageTask = Task.Factory.StartNew((cs) => SendMessages(cs as CancellationTokenSource),
                cancellationTokenSource,
                cancellationTokenSource.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        public virtual void Stop()
        {
            if (_sendMessageTask != null)
            {
                CancellationTokenSource cancellationSource = _sendMessageTask.AsyncState as CancellationTokenSource;
                cancellationSource.Cancel(true);
                Task.WaitAll(_sendMessageTask);
            }
        }

        public void Send(params IMessage[] messages)
        {
            messages.ForEach(message => _messageQueue.Add(_messageQueueClient.WrapMessage(message)));
        }

        public void Send(params Message.IMessageContext[] messageContexts)
        {
            messageContexts.ForEach(@messageContext => _messageQueue.Add(messageContext));
        }

        void SendMessages(CancellationTokenSource cancellationTokenSource)
        {
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    var messageContext = _messageQueue.Take(cancellationTokenSource.Token);
                    while (true)
                    {
                        try
                        {
                            Send(messageContext, messageContext.Topic ?? _defaultTopic);
                            CompleteSendingMessage(messageContext.MessageID);
                            break;
                        }
                        catch (Exception)
                        {
                            Thread.Sleep(1000);
                        }
                    }
                }
                catch(OperationCanceledException)
                {
                    return;
                }
                catch (ThreadAbortException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.Error(ex);
                }
            }
        }
    }
}
