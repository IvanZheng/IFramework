using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IFramework.Infrastructure.Mailboxes.Impl
{
    public class ProcessingMailbox<TMessage>
            where TMessage : class
    {
        readonly IProcessingMessageScheduler<TMessage> _scheduler;
        internal ConcurrentQueue<TMessage> MessageQueue { get; private set; }
        Func<TMessage, Task> _processMessage;
        Action<ProcessingMailbox<TMessage>> _handleMailboxEmpty;
        public string Key { get; private set; }
        private volatile int _isHandlingMessage;
        private int _batchCount;
        static int _processedCount;
        public static int ProcessedCount
        {
            get
            {
                return _processedCount;
            }
        }

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
            int processedCount = 0;
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
                if (_handleMailboxEmpty != null)
                {
                    _handleMailboxEmpty(this);
                }
            }
        }

        private void RegisterForExecution()
        {
            _scheduler.ScheduleMailbox(this);
        }

    }
}
