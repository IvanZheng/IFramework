using System;
using System.Threading.Tasks;

namespace IFramework.Infrastructure.Mailboxes.Impl
{
    public class DefaultProcessingMessageScheduler : IProcessingMessageScheduler
    {
        public Task ScheduleMailbox(Mailbox mailbox)
        {
            if (mailbox.EnterHandlingMessage())
            {
                return mailbox.Run();
            }
            return Task.CompletedTask;
        }
    }
}