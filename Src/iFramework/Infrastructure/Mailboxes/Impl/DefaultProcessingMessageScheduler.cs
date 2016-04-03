using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IFramework.Infrastructure.Mailboxes.Impl
{
    public class DefaultProcessingMessageScheduler<TMessage> : IProcessingMessageScheduler<TMessage>
        where TMessage : class
    {
        public Task ScheduleMailbox(ProcessingMailbox<TMessage> mailbox)
        {
            Task task = null;
            if (mailbox.EnterHandlingMessage())
            {
                task = Task.Factory.StartNew(() => mailbox.Run(),
                CancellationToken.None,
                TaskCreationOptions.None,
                TaskScheduler.Default);
            }
            return task;
        }

        public Task SchedulProcessing(Action processing)
        {
            return Task.Factory.StartNew(processing,
                CancellationToken.None,
                TaskCreationOptions.None,
                TaskScheduler.Default);
        }
    }
}
