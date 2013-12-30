using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.Message
{
    public interface IMessageStore : IDisposable
    {
        void Save(IMessageContext commandContext, string domainEventID);
        void Save(IMessageContext commandContext, IEnumerable<IMessageContext> domainEventContexts);
    }
}
