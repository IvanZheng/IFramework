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
        void Process(TMessage messageContext, Action<TMessage> processingMessage);
    }
}
