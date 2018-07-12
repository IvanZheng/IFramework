using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IFramework.Infrastructure;
using IFramework.Message;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IFramework.MessageStores.Relational
{
    public class MessageStoreDaemon : IMessageStoreDaemon
    {
        private readonly ILogger<MessageStoreDaemon> _logger;
        private readonly MessageStore _messageStore;

        public enum MessageType
        {
            Command,
            Event
        }
        public class ToRemoveMessage
        {
            public string MessageId { get; }
            public MessageType Type { get; }

            public ToRemoveMessage(string messageId, MessageType type)
            {
                MessageId = messageId;
                Type = type;
            }
        }

        public MessageStoreDaemon(ILogger<MessageStoreDaemon> logger, IMessageStore messageStore)
        {
            _logger = logger;
            _messageStore = messageStore as MessageStore;
            if (_messageStore == null)
            {
                throw new ArgumentNullException(nameof(messageStore));
            }
           
            _messageStore.Database.AutoTransactionsEnabled = false;
            _messageStore.ChangeTracker.AutoDetectChangesEnabled = false;
        }

        private CancellationTokenSource _cancellationTokenSource;
        private Task _task;
        private readonly ConcurrentQueue<ToRemoveMessage> _toRemoveMessages = new ConcurrentQueue<ToRemoveMessage>();
        public void Dispose()
        {
            
        }

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
                        if (ConfigurationExtension.UseInMemoryDatabase)
                        {
                            toRemoveMessages.ForEach(rm =>
                            {
                                if (rm.Type == MessageType.Command)
                                {
                                    var removeCommand = _messageStore.UnSentCommands.Find(rm.MessageId);
                                    if (removeCommand != null)
                                    {
                                        _messageStore.RemoveEntity(removeCommand);
                                    }
                                }
                                else if (rm.Type == MessageType.Event)
                                {
                                    var removeEvent = _messageStore.UnPublishedEvents.Find(rm.MessageId);
                                    if (removeEvent != null)
                                    {
                                        _messageStore.RemoveEntity(removeEvent);
                                    }
                                }
                            });
                            _messageStore.SaveChanges();
                        }
                        else
                        {
                            var toRemoveCommands = toRemoveMessages.Where(rm => rm.Type == MessageType.Command)
                                                                   .Select(rm => rm.MessageId)
                                                                   .ToArray();
                            if (toRemoveCommands.Length > 0)
                            {
                                var deleteCommandsSql = $"delete from msgs_UnSentCommands where Id in ({string.Join(",", toRemoveCommands.Select(rm => $"'{rm}'"))})";
                                _messageStore.Database.ExecuteSqlCommand(deleteCommandsSql);
                            }
                            var toRemoveEvents = toRemoveMessages.Where(rm => rm.Type == MessageType.Event)
                                                                 .Select(rm => rm.MessageId)
                                                                 .ToArray();
                            if (toRemoveEvents.Length > 0)
                            {
                                var deleteEventsSql = $"delete from msgs_UnPublishedEvents where Id in ({string.Join(",", toRemoveEvents.Select(rm => $"'{rm}'"))})";
                                _messageStore.Database.ExecuteSqlCommand(deleteEventsSql);
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

        public void Stop()
        {
            if (_task != null)
            {
                _cancellationTokenSource.Cancel(true);
                _task.Wait();
            }
        }
    }
}
