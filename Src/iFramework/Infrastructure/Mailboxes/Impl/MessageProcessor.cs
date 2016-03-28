using IFramework.Infrastructure.Logging;
using IFramework.Message;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IFramework.Infrastructure.Mailboxes.Impl
{
    public class MessageProcessor : IMessageProcessor<IMessageContext>
    {
        IProcessingMessageScheduler<IMessageContext> _processingMessageScheduler;
        ConcurrentDictionary<string, ProcessingMailbox<IMessageContext>> _mailboxDict;
        public ConcurrentDictionary<string, ProcessingMailbox<IMessageContext>> MailboxDictionary
        {
            get
            {
                return _mailboxDict;
            }
        }

        BlockingCollection<IMailboxProcessorCommand> _mailboxProcessorCommands;
        CancellationTokenSource _cancellationSource;
        Task _processComandTask;
        ILogger _logger;

        public MessageProcessor(IProcessingMessageScheduler<IMessageContext> scheduler)
        {
            _logger = IoCFactory.Resolve<ILoggerFactory>().Create(this.GetType());

            _processingMessageScheduler = scheduler;
            _mailboxDict = new ConcurrentDictionary<string, ProcessingMailbox<IMessageContext>>();
            _mailboxProcessorCommands = new BlockingCollection<IMailboxProcessorCommand>();
            _cancellationSource = new CancellationTokenSource();
        }

        public void Start()
        {
            _processComandTask = Task.Factory.StartNew((cs) => ProcessMailboxProcessorCommands(cs as CancellationTokenSource),
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

        public void Process(IMessageContext messageContext, Action<IMessageContext> process)
        {
            _mailboxProcessorCommands.Add(new ProcessMessageCommand<IMessageContext>(messageContext, process));
        }

        private void ProcessMailboxProcessorCommands(CancellationTokenSource cancellationSource)
        {
            while (!cancellationSource.IsCancellationRequested)
            {
                try
                {
                    var command = _mailboxProcessorCommands.Take(cancellationSource.Token);
                    if (command is ProcessMessageCommand<IMessageContext>)
                    {
                        ExecuteProcessCommand((ProcessMessageCommand<IMessageContext>)command);
                    }
                    else if (command is CompleteMessageCommand<IMessageContext>)
                    {
                        CompleteProcessMessage((CompleteMessageCommand<IMessageContext>)command);
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
                    _logger.Error(ex.GetBaseException().Message, ex);
                }
            }
        }



        private void CompleteProcessMessage(CompleteMessageCommand<IMessageContext> command)
        {
            ProcessingMailbox<IMessageContext> mailbox = command.Mailbox;
            if (mailbox.MessageQueue.Count == 0)
            {
                _mailboxDict.TryRemove(mailbox.Key);
            }
        }

        private void HandleMailboxEmpty(ProcessingMailbox<IMessageContext> mailbox)
        {
            _mailboxProcessorCommands.Add(new CompleteMessageCommand<IMessageContext>(mailbox));
        }

        private void ExecuteProcessCommand(ProcessMessageCommand<IMessageContext> command)
        {
            var messageContext = command.Message;
            var processingMessage = command.ProcessingMessageAction;
            var key = messageContext.Key;

            if (!string.IsNullOrWhiteSpace(key))
            {
                var mailbox = _mailboxDict.GetOrAdd(key, x =>
                {
                    return new ProcessingMailbox<IMessageContext>(key, _processingMessageScheduler, processingMessage, HandleMailboxEmpty);
                });
                mailbox.EnqueueMessage(messageContext);
                _processingMessageScheduler.ScheduleMailbox(mailbox);
            }
            else
            {
                _processingMessageScheduler.SchedulProcessing(() => processingMessage(messageContext));
            }
        }
    }
}
