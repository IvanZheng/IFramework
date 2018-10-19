using System;
using System.Threading.Tasks;

namespace IFramework.Infrastructure.Mailboxes.Impl
{
    public class DefaultProcessingMessageScheduler<TMessage> : IProcessingMessageScheduler<TMessage>
        where TMessage : class
    {
        public async Task ScheduleMailbox(ProcessingMailbox<TMessage> mailbox)
        {
            if (mailbox.EnterHandlingMessage())
            {
                await mailbox.Run().ConfigureAwait(false);
            }
        }

        public async Task SchedulProcessing(Func<Task> processing)
        {
            await processing().ConfigureAwait(false);
        }
    }

    public class DefaultProcessingMessageScheduler : IProcessingMessageScheduler
    {
        public async Task ScheduleMailbox(Mailbox mailbox)
        {
            if (mailbox.EnterHandlingMessage())
            {
                await mailbox.Run().ConfigureAwait(false);
            }
        }

        public async Task SchedulProcessing(Func<Task> processing)
        {
            await processing().ConfigureAwait(false);
        }
    }
}