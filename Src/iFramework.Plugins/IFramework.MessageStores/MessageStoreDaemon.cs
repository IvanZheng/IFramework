using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IFramework.DependencyInjection;
using IFramework.Message;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IFramework.MessageStores.Relational
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
            }
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
                        _toRemoveMessages.TryDequeue(out toRemoveMessage);
                        if (toRemoveMessage != null)
                        {
                            toRemoveMessages.Add(toRemoveMessage);
                        }
                    } while (toRemoveMessages.Count < 10 && toRemoveMessage != null);

                    if (toRemoveMessages.Count == 0)
                    {
                        Task.Delay(1000).Wait();
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
                            if (messageStore.InMemoryStore)
                            {
                                toRemoveMessages.ForEach(rm =>
                                {
                                    if (rm.Type == MessageType.Command)
                                    {
                                        var removeCommand = messageStore.UnSentCommands.Find(rm.MessageId);
                                        if (removeCommand != null)
                                        {
                                            messageStore.RemoveEntity(removeCommand);
                                        }
                                    }
                                    else if (rm.Type == MessageType.Event)
                                    {
                                        var removeEvent = messageStore.UnPublishedEvents.Find(rm.MessageId);
                                        if (removeEvent != null)
                                        {
                                            messageStore.RemoveEntity(removeEvent);
                                        }
                                    }
                                });
                                messageStore.SaveChanges();
                            }
                            else
                            {
                                var toRemoveCommands = toRemoveMessages.Where(rm => rm.Type == MessageType.Command)
                                                                       .Select(rm => rm.MessageId)
                                                                       .ToArray();
                                if (toRemoveCommands.Length > 0)
                                {
                                    var deleteCommandsSql = $"delete from msgs_UnSentCommands where Id in ({string.Join(",", toRemoveCommands.Select(rm => $"'{rm}'"))})";
                                    messageStore.Database.ExecuteSqlCommand(deleteCommandsSql);
                                }

                                var toRemoveEvents = toRemoveMessages.Where(rm => rm.Type == MessageType.Event)
                                                                     .Select(rm => rm.MessageId)
                                                                     .ToArray();
                                if (toRemoveEvents.Length > 0)
                                {
                                    var deleteEventsSql = $"delete from msgs_UnPublishedEvents where Id in ({string.Join(",", toRemoveEvents.Select(rm => $"'{rm}'"))})";
                                    messageStore.Database.ExecuteSqlCommand(deleteEventsSql);
                                }
                            }
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