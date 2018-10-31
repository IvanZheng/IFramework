using System;
using System.Threading.Tasks;

namespace IFramework.Infrastructure.Mailboxes.Impl
{
    public class DefaultProcessingMessageScheduler : IProcessingMessageScheduler
    {
        public async Task ScheduleMailbox(Mailbox mailbox)
        {
            if (mailbox.EnterHandlingMessage())
            {
                await mailbox.Run().ConfigureAwait(false);
            }
        }

        public Task SchedulProcessing(Func<Task> processing)
        {
            return processing();
        }
    }
}