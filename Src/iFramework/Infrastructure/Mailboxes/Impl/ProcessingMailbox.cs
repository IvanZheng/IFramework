using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace IFramework.Infrastructure.Mailboxes.Impl
{
    public class ProcessingMailbox<TMessage>
        where TMessage : class
    {
        private static int _processedCount;
        private readonly int _batchCount;
        private readonly Action<ProcessingMailbox<TMessage>> _handleMailboxEmpty;
        private readonly Func<TMessage, Task> _processMessage;
        private readonly IProcessingMessageScheduler<TMessage> _scheduler;
        private volatile int _isHandlingMessage;

        public ProcessingMailbox(string key,
                                 IProcessingMessageScheduler<TMessage> scheduler,
                                 Func<TMessage, Task> processingMessage,
                                 Action<ProcessingMailbox<TMessage>> handleMailboxEmpty,
                                 int batchCount = 100)
        {
            _batchCount = batchCount;
            _scheduler = scheduler;
            _processMessage = processingMessage;
            _handleMailboxEmpty = handleMailboxEmpty;
            Key = key;
            MessageQueue = new ConcurrentQueue<TMessage>();
        }

        internal ConcurrentQueue<TMessage> MessageQueue { get; }
        public string Key { get; }

        public static int ProcessedCount => _processedCount;


        public void EnqueueMessage(TMessage processingMessage)
        {
            MessageQueue.Enqueue(processingMessage);
        }


        internal bool EnterHandlingMessage()
        {
            return Interlocked.CompareExchange(ref _isHandlingMessage, 1, 0) == 0;
        }

        internal async Task Run()
        {
            TMessage processingMessage = null;
            var processedCount = 0;
            while (processedCount < _batchCount)
            {
                try
                {
                    processingMessage = null;
                    if (MessageQueue.TryDequeue(out processingMessage))
                    {
                        await _processMessage(processingMessage).ConfigureAwait(false);
                    }
                    else
                    {
                        break;
                    }
                }
                finally
                {
                    processedCount++;
                    if (processingMessage != null)
                    {
                        Interlocked.Add(ref _processedCount, 1);
                    }
                }
            }
            ExitHandlingMessage();
        }


        private void ExitHandlingMessage()
        {
            Interlocked.Exchange(ref _isHandlingMessage, 0);
            if (!MessageQueue.IsEmpty)
            {
                RegisterForExecution();
            }
            else
            {
                _handleMailboxEmpty?.Invoke(this);
            }
        }

        private void RegisterForExecution()
        {
            _scheduler.ScheduleMailbox(this);
        }
    }
}