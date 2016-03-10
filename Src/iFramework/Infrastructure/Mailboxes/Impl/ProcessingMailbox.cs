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
        readonly ConcurrentQueue<TMessage> _messageQueue;
        Action<TMessage> _processingMessage;
        private volatile int _isHandlingMessage;

        public ProcessingMailbox(IProcessingMessageScheduler<TMessage> scheduler, Action<TMessage> processingMessage)
        {
            _scheduler = scheduler;
            _processingMessage = processingMessage;
            _messageQueue = new ConcurrentQueue<TMessage>();
        }


        public void EnqueueMessage(TMessage processingMessage)
        {
            _messageQueue.Enqueue(processingMessage);
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
                if (_messageQueue.TryDequeue(out processingMessage))
                {
                    _processingMessage(processingMessage);
                }
            }
            finally
            {
                ExitHandlingMessage();
                if (!_messageQueue.IsEmpty)
                {
                    RegisterForExecution();
                }
            }
        }


        private void ExitHandlingMessage()
        {
            Interlocked.Exchange(ref _isHandlingMessage, 0);
        }

        private void RegisterForExecution()
        {
            _scheduler.ScheduleMailbox(this);
        }

    }
}
