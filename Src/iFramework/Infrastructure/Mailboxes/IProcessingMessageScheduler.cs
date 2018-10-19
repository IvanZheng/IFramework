using System;
using System.Threading.Tasks;
using IFramework.Infrastructure.Mailboxes.Impl;

namespace IFramework.Infrastructure.Mailboxes
{
    public interface IProcessingMessageScheduler<TMessage>
        where TMessage : class
    {
        Task ScheduleMailbox(ProcessingMailbox<TMessage> mailbox);
        Task SchedulProcessing(Func<Task> processing);
    }

    public interface IProcessingMessageScheduler
    {
        Task ScheduleMailbox(Mailbox mailbox);
        Task SchedulProcessing(Func<Task> processing);
    }
}