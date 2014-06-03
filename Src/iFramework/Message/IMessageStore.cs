using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.Message
{
    public interface IMessageStore : IDisposable
    {
        void SaveDomainEvent(IMessageContext domainEventContext, IEnumerable<IMessageContext> commandContexts);
        void SaveCommand(IMessageContext commandContext, IEnumerable<IMessageContext> domainEventContexts);
    }
}
