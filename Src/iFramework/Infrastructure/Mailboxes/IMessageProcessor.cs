using IFramework.Infrastructure.Mailboxes.Impl;
using IFramework.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.Infrastructure.Mailboxes
{
    public interface IMessageProcessor<TMessage>
        where TMessage : class
    {
        void Start();
        void Stop();
        void Process(ProcessMessageCommand<TMessage> command);
    }
}
