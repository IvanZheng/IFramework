using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace IFramework.Infrastructure.Mailboxes.Impl
{
    public class Mailbox
    {
        private readonly int _batchCount;
        private readonly IProcessingMessageScheduler _scheduler;
        private volatile int _isHandlingMessage;

        public Mailbox(string key,
                       IProcessingMessageScheduler scheduler,
                       int batchCount = 100)
        {
            _batchCount = batchCount;
            _scheduler = scheduler;
            Key = key;
            MessageQueue = new ConcurrentQueue<Func<Task>>();
        }

        internal ConcurrentQueue<Func<Task>> MessageQueue { get; }
        public string Key { get; }


        public void EnqueueMessage(Func<Task> processing)
        {
            MessageQueue.Enqueue(processing);
        }


        internal async Task Run()
        {
            var processedCount = 0;
            while (processedCount < _batchCount)
            {
                try
                {
                    if (MessageQueue.TryDequeue(out var processingMessage))
                    {
                        await processingMessage().ConfigureAwait(false);
                    }
                    else
                    {
                        break;
                    }
                }
                finally
                {
                    processedCount++;
                }
            }

            ExitHandlingMessage();
        }


        public bool EnterHandlingMessage()
        {
            return Interlocked.CompareExchange(ref _isHandlingMessage, 1, 0) == 0;
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
                //_handleMailboxEmpty?.Invoke(this);
            }
        }

        private void RegisterForExecution()
        {
            _scheduler.ScheduleMailbox(this);
        }
    }
}