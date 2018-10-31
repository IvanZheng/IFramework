using System;
using System.Threading.Tasks;
using IFramework.Infrastructure.Mailboxes.Impl;

namespace IFramework.Infrastructure.Mailboxes
{
    public interface IProcessingMessageScheduler
    {
        Task ScheduleMailbox(Mailbox mailbox);
    }
}