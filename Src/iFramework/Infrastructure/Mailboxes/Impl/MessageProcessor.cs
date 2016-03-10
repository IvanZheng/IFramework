using IFramework.Message;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.Infrastructure.Mailboxes.Impl
{
    public class MessageProcessor : IMessageProcessor<IMessageContext>
    {
        IProcessingMessageScheduler<IMessageContext> _processingMessageScheduler;
        ConcurrentDictionary<string, ProcessingMailbox<IMessageContext>> _mailboxDict;
        BlockingCollection<MailboxProcessorCommand> _mailboxProcessorCommands;
        public MessageProcessor(IProcessingMessageScheduler<IMessageContext> scheduler)
        {
            _processingMessageScheduler = scheduler;
            _mailboxDict = new ConcurrentDictionary<string, ProcessingMailbox<IMessageContext>>();
            _mailboxProcessorCommands = new BlockingCollection<MailboxProcessorCommand>();

        }
     



        public void CompleteProcessMessage(ProcessingMailbox<IMessageContext> mailbox)
        {
            if (mailbox.MessageQueue.Count == 0)
            {
                _mailboxDict.TryRemove(mailbox.Key);
            }
        }


        public void Process(IMessageContext messageContext, Action<IMessageContext> processingMessage)
        {
            var key = messageContext.Key;
            if (!string.IsNullOrWhiteSpace(key))
            {
                var mailbox = _mailboxDict.GetOrAdd(key, x => 
                {
                    return new ProcessingMailbox<IMessageContext>(key, _processingMessageScheduler, processingMessage);
                });
                mailbox.EnqueueMessage(messageContext);
                _processingMessageScheduler.ScheduleMailbox(mailbox);
            }
            else
            {
                _processingMessageScheduler.SchedulProcessing(() => processingMessage(messageContext));
            }
        }

       
    }
}
