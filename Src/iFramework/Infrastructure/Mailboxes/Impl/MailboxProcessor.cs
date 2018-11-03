using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IFramework.Message;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IFramework.Infrastructure.Mailboxes.Impl
{
    public class MailboxProcessor : IMailboxProcessor
    {
        private readonly IProcessingMessageScheduler _scheduler;
        private readonly ILogger _logger;
        private readonly int _batchCount;
        private Task _processComandTask;
        private readonly CancellationTokenSource _cancellationSource;
        private readonly ConcurrentDictionary<string, Mailbox> _mailboxDictionary;

        private readonly BlockingCollection<IMailboxProcessorCommand> _mailboxProcessorCommands;

        public string Status => string.Join(", ", _mailboxDictionary.Select(e => $"[{e.Key}: {e.Value.MessageQueue.Count}]"));

        public MailboxProcessor(IProcessingMessageScheduler scheduler, IOptions<MailboxOption> options, ILogger<MailboxProcessor> logger)
        {
            _scheduler = scheduler;
            _logger = logger;
            _batchCount = options.Value.BatchCount;
            _mailboxProcessorCommands = new BlockingCollection<IMailboxProcessorCommand>();
            _mailboxDictionary = new ConcurrentDictionary<string, Mailbox>();
            _cancellationSource = new CancellationTokenSource();
        }

        public void Start()
        {
            _processComandTask = Task.Factory.StartNew(cs => ProcessMailboxProcessorCommands(cs as CancellationTokenSource),
                                                       _cancellationSource,
                                                       _cancellationSource.Token,
                                                       TaskCreationOptions.LongRunning,
                                                       TaskScheduler.Default);
        }

        private void ExecuteProcessCommand(ProcessMessageCommand command)
        {
            var key = command.Message.Key;
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(key));
            }
            var mailbox = _mailboxDictionary.GetOrAdd(key, x =>
            {
                var box = new Mailbox(key, _scheduler, _batchCount);
                box.OnMessageEmpty += Mailbox_OnMessageEmpty;
                return box;
            });
           
            mailbox.EnqueueMessage(command.Message);
            _scheduler.ScheduleMailbox(mailbox);
        }

        private void Mailbox_OnMessageEmpty(Mailbox mailbox)
        {
            _mailboxProcessorCommands.Add(new CompleteMessageCommand(mailbox));
        }

        private void ProcessMailboxProcessorCommands(CancellationTokenSource cancellationSource)
        {
            while (!cancellationSource.IsCancellationRequested)
            {
                try
                {
                    var command = _mailboxProcessorCommands.Take(cancellationSource.Token);
                    if (command is ProcessMessageCommand messageCommand)
                    {
                        ExecuteProcessCommand(messageCommand);
                    }
                    else if (command is CompleteMessageCommand completeMessageCommand)
                    {
                        CompleteProcessMessage(completeMessageCommand);
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
                    _logger.LogError(ex, $"Message processor ProcessMailboxProcessorCommands failed");
                }
            }
        }

        private void CompleteProcessMessage(CompleteMessageCommand command)
        {
            var mailbox = command.Mailbox;
            if (mailbox.MessageQueue.Count == 0)
            {
                _mailboxDictionary.TryRemove(mailbox.Key);
            }
        }

        public void Stop()
        {
            if (_processComandTask != null)
            {
                _cancellationSource.Cancel(true);
                Task.WaitAll(_processComandTask);
            }
        }

        public Task<T> Process<T>(string key, Func<Task<T>> process, TaskCompletionSource<object> taskCompletionSource = null)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return process();
            }
            else
            {
                var mailboxMessage = new MailboxMessage(key, process, taskCompletionSource);
                _mailboxProcessorCommands.Add(new ProcessMessageCommand(mailboxMessage));
                return mailboxMessage.TaskCompletionSource.Task
                                     .ContinueWith(t => (T)t.Result);
            }
        }

        public Task Process(string key, Func<Task> process, TaskCompletionSource<object> taskCompletionSource = null)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return process();
            }
            else
            {
                var mailboxMessage = new MailboxMessage(key, process, taskCompletionSource);
                _mailboxProcessorCommands.Add(new ProcessMessageCommand(mailboxMessage));
                return mailboxMessage.TaskCompletionSource.Task;
            }
        }
    }
}
