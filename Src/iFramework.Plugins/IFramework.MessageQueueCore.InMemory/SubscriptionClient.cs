
using System;
using IFramework.Message;
using IFramework.MessageQueue;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using IFramework.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IFramework.MessageQueueCore.InMemory
{
    public class SubscriptionClient : IMessageConsumer
    {
        private readonly BlockingCollection<IMessageContext> _messageQueue = new BlockingCollection<IMessageContext>();
        private string _topic;
        private string _subscriptionName;
        private readonly OnMessagesReceived _onMessagesReceived;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _consumerTask;
        protected static ILogger Logger = ObjectProviderFactory.GetService<ILoggerFactory>().CreateLogger(nameof(SubscriptionClient));

        public SubscriptionClient(string topic, string subscriptionName, string consumerId, OnMessagesReceived onMessagesReceived, bool start = true)
        {
            _topic = topic;
            _subscriptionName = subscriptionName;
            Id = consumerId;
            _onMessagesReceived = onMessagesReceived;
            if (start)
            {
                Start();
            }
        }

        public void Enqueue(IMessageContext messageContext, CancellationToken cancellationToken)
        {
            _messageQueue.Add(messageContext, cancellationToken);
        }

        public void CommitOffset(IMessageContext messageContext)
        {
        }

        public void Start()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _consumerTask = Task.Factory.StartNew(cs => ReceiveMessages(cs as CancellationTokenSource),
                                                  _cancellationTokenSource,
                                                  _cancellationTokenSource.Token,
                                                  TaskCreationOptions.LongRunning,
                                                  TaskScheduler.Default);
        }

        public void Stop()
        {
            _cancellationTokenSource?.Cancel(true);
            _consumerTask?.Wait();
            _consumerTask?.Dispose();
            _consumerTask = null;
            _cancellationTokenSource = null;
        }

        public string Id { get; set; }


        private void ReceiveMessages(CancellationTokenSource cancellationTokenSource)
        {
            #region peek messages that not been consumed since last time
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    var messageContext = _messageQueue.Take();
                    _onMessagesReceived(messageContext);
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
                    if (!cancellationTokenSource.IsCancellationRequested)
                    {
                        Task.Delay(1000).Wait();
                        Logger.LogError(ex, $"SubscriptionClient {Id} ReceiveMessages failed!");
                    }
                }
            }

            #endregion
        }
    }
}
