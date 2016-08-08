using IFramework.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.Infrastructure.Mailboxes.Impl
{
    interface IMailboxProcessorCommand
    {

    }

    class ProcessMessageCommand<TMessage> : IMailboxProcessorCommand
        where TMessage : class
    {
        public TMessage Message { get; private set; }
        public Func<TMessage, Task> ProcessingMessageFunc { get; private set; }

        public ProcessMessageCommand(TMessage message, Func<TMessage, Task> processingMessageFunc)
        {
            Message = message;
            ProcessingMessageFunc = processingMessageFunc;
        }

    }

    class CompleteMessageCommand<TMessage> : IMailboxProcessorCommand
        where TMessage : class
    {
        public ProcessingMailbox<TMessage> Mailbox { get; private set; }

        public CompleteMessageCommand(ProcessingMailbox<TMessage> mailbox)
        {
            Mailbox = mailbox;
        }
    }
}
