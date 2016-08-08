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
        Task ScheduleMailbox(ProcessingMailbox<TMessage> mailbox);
        Task SchedulProcessing(Func<Task> processing);
    }
}
