using IFramework.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.Infrastructure.Mailboxes.Impl
{
    public interface IMailboxProcessorCommand
    {

    }

    public class ProcessMessageCommand<TMessage> : IMailboxProcessorCommand
        where TMessage : class
    {
        public TMessage Message { get; private set; }
        public Action<TMessage> ProcessingMessageAction { get; private set; }

        public ProcessMessageCommand(TMessage message, Action<TMessage> processingMessageAction)
        {
            Message = message;
            ProcessingMessageAction = processingMessageAction;
        }

    }

    public class CompleteMessageCommand<TMessage> : IMailboxProcessorCommand
        where TMessage : class
    {
        public ProcessingMailbox<TMessage> Mailbox { get; private set; }

        public CompleteMessageCommand(ProcessingMailbox<TMessage> mailbox)
        {
            Mailbox = mailbox;
        }
    }
}
