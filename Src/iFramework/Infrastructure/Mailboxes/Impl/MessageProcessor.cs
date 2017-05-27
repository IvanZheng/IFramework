using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using IFramework.Infrastructure.Logging;
using IFramework.IoC;
using IFramework.Message;

namespace IFramework.Infrastructure.Mailboxes.Impl
{
    public class MessageProcessor : IMessageProcessor<IMessageContext>
    {
        private readonly int _batchCount;
        private readonly CancellationTokenSource _cancellationSource;
        private readonly ILogger _logger;

        private readonly BlockingCollection<IMailboxProcessorCommand> _mailboxProcessorCommands;
        private Task _processComandTask;
        private readonly IProcessingMessageScheduler<IMessageContext> _processingMessageScheduler;

        public MessageProcessor(IProcessingMessageScheduler<IMessageContext> scheduler, int batchCount = 100)
        {
            _logger = IoCFactory.IsInit() ? IoCFactory.Resolve<ILoggerFactory>().Create(GetType()) : null;
            _batchCount = batchCount;
            _processingMessageScheduler = scheduler;
            MailboxDictionary = new ConcurrentDictionary<string, ProcessingMailbox<IMessageContext>>();
            _mailboxProcessorCommands = new BlockingCollection<IMailboxProcessorCommand>();
            _cancellationSource = new CancellationTokenSource();
        }

        public ConcurrentDictionary<string, ProcessingMailbox<IMessageContext>> MailboxDictionary { get; }

        public void Start()
        {
            _processComandTask = Task.Factory.StartNew(
                cs => ProcessMailboxProcessorCommands(cs as CancellationTokenSource),
                _cancellationSource,
                _cancellationSource.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        public void Stop()
        {
            if (_processComandTask != null)
            {
                _cancellationSource.Cancel(true);
                Task.WaitAll(_processComandTask);
            }
        }

        public void Process(IMessageContext messageContext, Func<IMessageContext, Task> process)
        {
            _mailboxProcessorCommands.Add(new ProcessMessageCommand<IMessageContext>(messageContext, process));
        }

        private void ProcessMailboxProcessorCommands(CancellationTokenSource cancellationSource)
        {
            while (!cancellationSource.IsCancellationRequested)
                try
                {
                    var command = _mailboxProcessorCommands.Take(cancellationSource.Token);
                    if (command is ProcessMessageCommand<IMessageContext>)
                        ExecuteProcessCommand((ProcessMessageCommand<IMessageContext>) command);
                    else if (command is CompleteMessageCommand<IMessageContext>)
                        CompleteProcessMessage((CompleteMessageCommand<IMessageContext>) command);
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
                    _logger?.Error(ex.GetBaseException().Message, ex);
                }
        }


        private void CompleteProcessMessage(CompleteMessageCommand<IMessageContext> command)
        {
            var mailbox = command.Mailbox;
            if (mailbox.MessageQueue.Count == 0)
                MailboxDictionary.TryRemove(mailbox.Key);
        }

        private void HandleMailboxEmpty(ProcessingMailbox<IMessageContext> mailbox)
        {
            _mailboxProcessorCommands.Add(new CompleteMessageCommand<IMessageContext>(mailbox));
        }

        private void ExecuteProcessCommand(ProcessMessageCommand<IMessageContext> command)
        {
            var messageContext = command.Message;
            var processingMessageFunc = command.ProcessingMessageFunc;
            var key = messageContext.Key;

            if (!string.IsNullOrWhiteSpace(key))
            {
                var mailbox = MailboxDictionary.GetOrAdd(key,
                    x =>
                    {
                        return new ProcessingMailbox<IMessageContext>(key, _processingMessageScheduler,
                            processingMessageFunc, HandleMailboxEmpty, _batchCount);
                    });
                mailbox.EnqueueMessage(messageContext);
                _processingMessageScheduler.ScheduleMailbox(mailbox);
            }
            else
            {
                _processingMessageScheduler.SchedulProcessing(() => processingMessageFunc(messageContext));
            }
        }
    }
}