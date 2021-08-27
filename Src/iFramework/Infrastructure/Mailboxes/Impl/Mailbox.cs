using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace IFramework.Infrastructure.Mailboxes.Impl
{
    public class Mailbox
    {
        public delegate void MessageEmptyHandler(Mailbox mailbox);

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
            MessageQueue = new ConcurrentQueue<MailboxMessage>();
        }

        internal ConcurrentQueue<MailboxMessage> MessageQueue { get; }
        public string Key { get; }
        public event MessageEmptyHandler OnMessageEmpty;


        public void EnqueueMessage(MailboxMessage message)
        {
            MessageQueue.Enqueue(message);
        }


        internal async Task Run()
        {
            var processedCount = 0;
            while (processedCount < _batchCount)
            {
                MailboxMessage processingMessage = null;
                try
                {
                    if (MessageQueue.TryDequeue(out processingMessage))
                    {
                        processedCount++;
                        var task = processingMessage.Task();
                        await task.ConfigureAwait(false);
                        object returnValue = null;
                        if (processingMessage.HasReturnValue)
                        {
                            returnValue = ((dynamic) task).Result;
                        }
                        processingMessage.TaskCompletionSource
                                         .TrySetResult(returnValue);
                    }
                    else
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    processingMessage?.TaskCompletionSource
                                     .TrySetException(ex);
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
                OnMessageEmpty?.Invoke(this);
            }
        }

        private void RegisterForExecution()
        {
            _scheduler.ScheduleMailbox(this);
        }
    }
}