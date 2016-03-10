using IFramework.Infrastructure.Mailboxes.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.Infrastructure.Mailboxes
{
    public interface IProcessingMessageScheduler<TMessage>
        where TMessage : class
    {
        void ScheduleMailbox(ProcessingMailbox<TMessage> mailbox);
        void SchedulProcessing(Action processing);
    }
}
