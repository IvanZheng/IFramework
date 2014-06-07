using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.Message.Impl
{
    class MockMessageStore : IMessageStore
    {
        public bool HasCommandHandled(string commandId)
        {
            return false;
        }

        public bool HasEventHandled(string eventId, string subscriptionName)
        {
            return false;
            
        }

        public void SaveEvent(IMessageContext eventContext, string subscriptionName, IEnumerable<IMessageContext> commandContexts)
        {
        }

        public void SaveCommand(IMessageContext commandContext, IEnumerable<IMessageContext> eventContexts)
        {
           
        }

        public void SaveFailedCommand(IMessageContext commandContext)
        {
            
        }

        public void RemoveSentCommand(string commandId)
        {
            
        }

        public void RemovePublishedEvent(string eventId)
        {
           
        }

        public IEnumerable<IMessageContext> GetAllUnSentCommands()
        {
            return null;
        }

        public IEnumerable<IMessageContext> GetAllUnPublishedEvents()
        {
            return null;
        }

        public void Dispose()
        {
            
        }
    }
}
