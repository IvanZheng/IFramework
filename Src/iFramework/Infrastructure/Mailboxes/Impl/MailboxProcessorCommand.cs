using System;
using System.Threading.Tasks;

namespace IFramework.Infrastructure.Mailboxes.Impl
{
    internal interface IMailboxProcessorCommand
    {
    }

    internal class ProcessMessageCommand<TMessage> : IMailboxProcessorCommand
        where TMessage : class
    {
        public ProcessMessageCommand(TMessage message, Func<TMessage, Task> processingMessageFunc)
        {
            Message = message;
            ProcessingMessageFunc = processingMessageFunc;
        }

        public TMessage Message { get; }
        public Func<TMessage, Task> ProcessingMessageFunc { get; }
    }

    internal class CompleteMessageCommand<TMessage> : IMailboxProcessorCommand
        where TMessage : class
    {
        public CompleteMessageCommand(ProcessingMailbox<TMessage> mailbox)
        {
            Mailbox = mailbox;
        }

        public ProcessingMailbox<TMessage> Mailbox { get; }
    }
}