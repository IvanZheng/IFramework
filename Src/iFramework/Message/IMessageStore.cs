using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.Message
{
    public interface IMessageStore : IDisposable
    {
        bool HasCommandHandled(string commandID);
        bool HasEventHandled(string eventID);
        void SaveEvent(IMessageContext eventContext, IEnumerable<IMessageContext> commandContexts);
        void SaveCommand(IMessageContext commandContext, IEnumerable<IMessageContext> eventContexts);
    }
}
