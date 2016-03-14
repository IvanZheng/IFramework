using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                task = Task.Run(() => mailbox.Run());
            }
            return task;
        }

        public Task SchedulProcessing(Action processing)
        {
            return Task.Run(processing);
        }
    }
}
