using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IFramework.Config;
using IFramework.Infrastructure;
using IFramework.DependencyInjection;
using IFramework.MessageQueue;
using Microsoft.Extensions.Logging;

namespace IFramework.Message.Impl
{
    public abstract class MessageSender: IMessageSender
    {
        protected string DefaultTopic;
        protected ILogger Logger;
        protected IMessageQueueClient MessageQueueClient;
        protected static bool NeedMessageStore;
        protected Task SendMessageTask;

        static MessageSender()
        {
            NeedMessageStore = Configuration.Instance.NeedMessageStore;
        }

        protected MessageSender(IMessageQueueClient messageQueueClient, string defaultTopic = null)
        {
            MessageQueueClient = messageQueueClient;
            DefaultTopic = defaultTopic;
            MessageStateQueue = new BlockingCollection<MessageState>();
            Logger = ObjectProviderFactory.GetService<ILoggerFactory>().CreateLogger(GetType());
        }

        protected BlockingCollection<MessageState> MessageStateQueue { get; set; }

        public virtual void Start()
        {
            if (NeedMessageStore)
            {
                ObjectProviderFactory.GetService<IMessageStoreDaemon>().Start();
                GetAllUnSentMessages().ForEach(eventContext => MessageStateQueue.Add(new MessageState(eventContext)));
            }
            var cancellationTokenSource = new CancellationTokenSource();
            SendMessageTask = Task.Factory.StartNew(cs => SendMessages(cs as CancellationTokenSource),
                                                     cancellationTokenSource,
                                                     cancellationTokenSource.Token,
                                                     TaskCreationOptions.LongRunning,
                                                     TaskScheduler.Default);
        }

        public virtual void Stop()
        {
            if (SendMessageTask != null)
            {
                if (NeedMessageStore)
                { 
                    ObjectProviderFactory.GetService<IMessageStoreDaemon>().Stop();
                }
                var cancellationSource = SendMessageTask.AsyncState as CancellationTokenSource;
                cancellationSource?.Cancel(true);
                Task.WaitAll(SendMessageTask);
            }
        }
        

        public Task<MessageResponse[]> SendAsync(CancellationToken cancellationToken, params IMessage[] messages)
        {
            var sendTaskCompletionSource = new TaskCompletionSource<MessageResponse>();
            if (cancellationToken != CancellationToken.None)
            {
                cancellationToken.Register(OnSendCancel, sendTaskCompletionSource);
            }
            var messageStates = messages.Select(message =>
                                        {
                                            var topic = message.GetFormatTopic();
                                            return new MessageState(MessageQueueClient.WrapMessage(message, topic: topic, key: message.Key),
                                                                    sendTaskCompletionSource,
                                                                    false);
                                        })
                                        .ToArray();
            return SendAsync(cancellationToken, messageStates);
        }


        public Task<MessageResponse[]> SendAsync(CancellationToken cancellationToken, params MessageState[] messageStates)
        {
            messageStates.ForEach(messageState =>
            {
                MessageStateQueue.Add(messageState, cancellationToken);
                Logger.LogDebug($"send message enqueue msgId: {messageState.MessageID} topic:{messageState.MessageContext.Topic}");
            });
            return Task.WhenAll(messageStates.Where(s => s.SendTaskCompletionSource != null)
                                             .Select(s => s.SendTaskCompletionSource.Task)
                                             .ToArray());
        }

        protected abstract IEnumerable<IMessageContext> GetAllUnSentMessages();
        protected abstract Task SendMessageStateAsync(MessageState messageState, CancellationToken cancellationToken);
        protected abstract void CompleteSendingMessage(MessageState messageState);


        protected virtual void OnSendCancel(object state)
        {
            var sendTaskCompletionSource = state as TaskCompletionSource<MessageResponse>;
            sendTaskCompletionSource?.TrySetCanceled();
        }

        private void SendMessages(CancellationTokenSource cancellationTokenSource)
        {
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    var messageState = MessageStateQueue.Take(cancellationTokenSource.Token);
                    SendMessageStateAsync(messageState, cancellationTokenSource.Token);
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
                    Logger.LogError(ex, $"SendMessages Processing faield!");
                }
            }
        }
    }
}