﻿using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using IFramework.DependencyInjection;
using IFramework.Message;
using IFramework.MessageQueue;
using Microsoft.Extensions.Logging;

namespace IFramework.MessageQueue.InMemory
{
    public class QueueClient : IMessageConsumer
    {
        protected static ILogger Logger = ObjectProviderFactory.GetService<ILoggerFactory>().CreateLogger<QueueClient>();
        private readonly BlockingCollection<IMessageContext> _messageQueue;
        private readonly OnMessagesReceived _onMessagesReceived;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _consumerTask;
        private string _queue;

        public QueueClient(string queue,
                           string consumerId,
                           OnMessagesReceived onMessagesReceived,
                           BlockingCollection<IMessageContext> messageQueue,
                           bool start = true)
        {
            _queue = queue;
            _messageQueue = messageQueue;
            Id = consumerId;
            _onMessagesReceived = onMessagesReceived;
            if (start)
            {
                Start();
            }
        }

        public void CommitOffset(IMessageContext messageContext) { }

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

        public string Status => $"{Id}: messageCount: {_messageQueue.Count}";

        private void ReceiveMessages(CancellationTokenSource cancellationTokenSource)
        {
            #region peek messages that not been consumed since last time

            while (!cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    var messageContext = _messageQueue.Take(cancellationTokenSource.Token);
                    _onMessagesReceived(cancellationTokenSource.Token, messageContext);
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
                        Logger.LogError(ex, $"QueueClient {Id} ReceiveMessages failed!");
                    }
                }
            }

            #endregion
        }
    }
}