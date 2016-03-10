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
        public void ScheduleMailbox(ProcessingMailbox<TMessage> mailbox)
        {
            if (mailbox.EnterHandlingMessage())
            {
               Task.Run(() => mailbox.Run());
            }
        }

        public void SchedulProcessing(Action processing)
        {
            Task.Run(processing);
        }
    }
}
