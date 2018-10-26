using System;
using System.Threading.Tasks;

namespace IFramework.Infrastructure.Mailboxes.Impl
{
    internal interface IMailboxProcessorCommand { }

    internal class ProcessMessageCommand : IMailboxProcessorCommand
    {
        public MailboxMessage Message { get; private set; }

        public ProcessMessageCommand(MailboxMessage message)
        {
            Message = message;
        }
    }

    internal class CompleteMessageCommand : IMailboxProcessorCommand
    {
        public CompleteMessageCommand(Mailbox mailbox)
        {
            Mailbox = mailbox;
        }

        public Mailbox Mailbox { get; set; }
    }
}