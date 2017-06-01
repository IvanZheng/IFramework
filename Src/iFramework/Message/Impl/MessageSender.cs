using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IFramework.Config;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Logging;
using IFramework.IoC;
using IFramework.MessageQueue;

namespace IFramework.Message.Impl
{
    public abstract class MessageSender : IMessageSender
    {
        protected string _defaultTopic;
        protected ILogger _logger;
        protected IMessageQueueClient _messageQueueClient;
        protected bool _needMessageStore;
        protected Task _sendMessageTask;

        public MessageSender(IMessageQueueClient messageQueueClient, string defaultTopic = null)
        {
            _messageQueueClient = messageQueueClient;
            _defaultTopic = defaultTopic;
            _needMessageStore = Configuration.Instance.NeedMessageStore;
            _messageStateQueue = new BlockingCollection<MessageState>();
            _logger = IoCFactory.IsInit() ? IoCFactory.Resolve<ILoggerFactory>().Create(GetType()) : null;
        }

        protected BlockingCollection<MessageState> _messageStateQueue { get; set; }

        public virtual void Start()
        {
            if (_needMessageStore)
            {
                GetAllUnSentMessages().ForEach(eventContext => _messageStateQueue.Add(new MessageState(eventContext)));
            }
            var cancellationTokenSource = new CancellationTokenSource();
            _sendMessageTask = Task.Factory.StartNew(cs => SendMessages(cs as CancellationTokenSource),
                                                     cancellationTokenSource,
                                                     cancellationTokenSource.Token,
                                                     TaskCreationOptions.LongRunning,
                                                     TaskScheduler.Default);
        }

        public virtual void Stop()
        {
            if (_sendMessageTask != null)
            {
                var cancellationSource = _sendMessageTask.AsyncState as CancellationTokenSource;
                cancellationSource.Cancel(true);
                Task.WaitAll(_sendMessageTask);
            }
        }

        public Task<MessageResponse[]> SendAsync(params IMessage[] messages)
        {
            return SendAsync(CancellationToken.None, messages);
        }

        public Task<MessageResponse[]> SendAsync(CancellationToken sendCancellationToken, params IMessage[] messages)
        {
            var sendTaskCompletionSource = new TaskCompletionSource<MessageResponse>();
            if (sendCancellationToken != CancellationToken.None)
            {
                sendCancellationToken.Register(OnSendCancel, sendTaskCompletionSource);
            }
            var messageStates = messages.Select(message =>
                                        {
                                            var topic = message.GetFormatTopic();
                                            return new MessageState(_messageQueueClient.WrapMessage(message, topic: topic, key: message.Key),
                                                                    sendTaskCompletionSource,
                                                                    false);
                                        })
                                        .ToArray();
            return SendAsync(messageStates);
        }


        public Task<MessageResponse[]> SendAsync(params MessageState[] messageStates)
        {
            messageStates.ForEach(messageState => { _messageStateQueue.Add(messageState); });
            return Task.WhenAll(messageStates.Where(s => s.SendTaskCompletionSource != null)
                                             .Select(s => s.SendTaskCompletionSource.Task)
                                             .ToArray());
        }

        protected abstract IEnumerable<IMessageContext> GetAllUnSentMessages();
        protected abstract void Send(IMessageContext messageContext, string topic);
        protected abstract void CompleteSendingMessage(MessageState messageState);


        protected virtual void OnSendCancel(object state)
        {
            var sendTaskCompletionSource = state as TaskCompletionSource<MessageResponse>;
            if (sendTaskCompletionSource != null)
            {
                sendTaskCompletionSource.TrySetCanceled();
            }
        }

        private void SendMessages(CancellationTokenSource cancellationTokenSource)
        {
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    var messageState = _messageStateQueue.Take(cancellationTokenSource.Token);
                    while (true)
                    {
                        try
                        {
                            var messageContext = messageState.MessageContext;
                            Send(messageContext, messageContext.Topic ?? _defaultTopic);
                            CompleteSendingMessage(messageState);
                            break;
                        }
                        catch (Exception ex)
                        {
                            _logger?.Error(ex);
                            Thread.Sleep(2000);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (ThreadAbortException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger?.Error(ex);
                }
            }
        }
    }
}