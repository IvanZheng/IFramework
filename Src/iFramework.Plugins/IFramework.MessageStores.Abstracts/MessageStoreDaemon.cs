using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IFramework.DependencyInjection;
using IFramework.Infrastructure;
using IFramework.Message;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IFramework.MessageStores.Abstracts
{
    public class MessageStoreDaemon : IMessageStoreDaemon
    {
        public enum MessageType
        {
            Command,
            Event
        }

        private readonly ILogger<MessageStoreDaemon> _logger;
        private readonly ConcurrentQueue<ToRemoveMessage> _toRemoveMessages = new ConcurrentQueue<ToRemoveMessage>();

        private CancellationTokenSource _cancellationTokenSource;
        private Task _task;

        public MessageStoreDaemon(ILogger<MessageStoreDaemon> logger)
        {
            _logger = logger;
        }

        public void Dispose() { }

        public void RemovePublishedEvent(string eventId)
        {
            _toRemoveMessages.Enqueue(new ToRemoveMessage(eventId, MessageType.Event));
        }

        public void RemoveSentCommand(string commandId)
        {
            _toRemoveMessages.Enqueue(new ToRemoveMessage(commandId, MessageType.Command));
        }

        public void Start()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _task = Task.Factory.StartNew(cs => RemoveMessages(cs as CancellationTokenSource),
                                          _cancellationTokenSource,
                                          _cancellationTokenSource.Token,
                                          TaskCreationOptions.LongRunning,
                                          TaskScheduler.Default);
        }

        public void Stop()
        {
            if (_task != null)
            {
                _cancellationTokenSource.Cancel(true);
                _task.Wait();
                _task = null;
            }
        }

        protected virtual void RemoveUnSentCommands(MessageStore messageStore, string[] toRemoveCommands)
        {
            toRemoveCommands.ForEach(messageId =>
            {
                var removeCommand = messageStore.UnSentCommands.Find(messageId);
                if (removeCommand != null)
                {
                    messageStore.RemoveEntity(removeCommand);
                }
            });
        }

        protected virtual void RemoveUnPublishedEvents(MessageStore messageStore, string[] toRemoveEvents)
        {
            toRemoveEvents.ForEach(messageId =>
            {
                var removeEvent = messageStore.UnPublishedEvents.Find(messageId);
                if (removeEvent != null)
                {
                    messageStore.RemoveEntity(removeEvent);
                }
            });
        }

        private void RemoveMessages(CancellationTokenSource cancellationTokenSource)
        {
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    var toRemoveMessages = new List<ToRemoveMessage>();
                    ToRemoveMessage toRemoveMessage = null;
                    do
                    {
                        if (_toRemoveMessages.TryDequeue(out toRemoveMessage))
                        {
                            toRemoveMessages.Add(toRemoveMessage);
                        }
                    } while (toRemoveMessages.Count < 10 && toRemoveMessage != null && !cancellationTokenSource.IsCancellationRequested);

                    if (toRemoveMessages.Count == 0 && !cancellationTokenSource.IsCancellationRequested)
                    {
                        Task.Delay(1000, cancellationTokenSource.Token).Wait();
                    }
                    else
                    {
                        using (var scope = ObjectProviderFactory.CreateScope())
                        {
                            var messageStore = scope.GetService<IMessageStore>() as MessageStore;
                            if (messageStore == null)
                            {
                                throw new Exception("invalid messagestore!");
                            }


                            var toRemoveCommands = toRemoveMessages.Where(rm => rm.Type == MessageType.Command)
                                                                   .Select(rm => rm.MessageId)
                                                                   .ToArray();
                            if (toRemoveCommands.Length > 0)
                            {
                                RemoveUnSentCommands(messageStore, toRemoveCommands);
                            }

                            var toRemoveEvents = toRemoveMessages.Where(rm => rm.Type == MessageType.Event)
                                                                 .Select(rm => rm.MessageId)
                                                                 .ToArray();
                            if (toRemoveEvents.Length > 0)
                            {
                                RemoveUnPublishedEvents(messageStore, toRemoveEvents);
                            }

                            messageStore.SaveChanges();
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
                    _logger.LogError(ex, $"remove Messages Processing faield!");
                }
            }
        }

        public class ToRemoveMessage
        {
            public ToRemoveMessage(string messageId, MessageType type)
            {
                MessageId = messageId;
                Type = type;
            }

            public string MessageId { get; }
            public MessageType Type { get; }
        }
    }
}