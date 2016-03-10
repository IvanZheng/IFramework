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
    public class ProcessingMailbox<TMessage>
        where TMessage : class
    {
        readonly IProcessingMessageScheduler<TMessage> _scheduler;
        internal ConcurrentQueue<TMessage> MessageQueue { get; private set; }
        Action<TMessage> _processingMessage;
        public string Key { get; private set; }
        private volatile int _isHandlingMessage;

        public ProcessingMailbox(string key, IProcessingMessageScheduler<TMessage> scheduler, Action<TMessage> processingMessage)
        {
            _scheduler = scheduler;
            _processingMessage = processingMessage;
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

        internal void Run()
        {
            TMessage processingMessage = null;
            try
            {
                if (MessageQueue.TryDequeue(out processingMessage))
                {
                    _processingMessage(processingMessage);
                }
            }
            finally
            {
                ExitHandlingMessage();
            }
        }


        private void ExitHandlingMessage()
        {
            Interlocked.Exchange(ref _isHandlingMessage, 0);
            if (!MessageQueue.IsEmpty)
            {
                RegisterForExecution();
            }
        }

        private void RegisterForExecution()
        {
            _scheduler.ScheduleMailbox(this);
        }

    }
}
