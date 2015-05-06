using IFramework.Event;
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

        public void SaveCommand(IMessageContext commandContext, params IMessageContext[] messageContexts)
        {
        }

        public void SaveFailedCommand(IMessageContext commandContext, Exception ex, IMessageContext reply = null)
        {
            
        }

        public void RemoveSentCommand(string commandId)
        {
            
        }

        public void RemovePublishedEvent(string eventId)
        {
           
        }

       

        public void Dispose()
        {
            
        }


        public void SaveEvent(IMessageContext eventContext, string subscriptionName, 
                              IEnumerable<IMessageContext> commandContexts, 
                              IEnumerable<IMessageContext> messageContexts)
        {
        }

        public void SaveFailHandledEvent(IMessageContext eventContext, string subscriptionName, Exception e)
        {
        }


        public IEnumerable<IMessageContext> GetAllUnSentCommands(Func<string, object, string, string, IMessageContext> wrapMessage)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IMessageContext> GetAllUnPublishedEvents(Func<string, object, string, string, IMessageContext> wrapMessage)
        {
            throw new NotImplementedException();
        }
    }
}
