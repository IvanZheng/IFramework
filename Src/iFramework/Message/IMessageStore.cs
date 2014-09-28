using IFramework.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.Message
{
    public interface IMessageStore : IDisposable
    {
        bool HasCommandHandled(string commandId);
        bool HasEventHandled(string eventId, string subscriptionName);
        void SaveEvent(IMessageContext eventContext, string subscriptionName, 
                       IEnumerable<IMessageContext> commandContexts,
                       IEnumerable<IMessageContext> messageContexts);

        void SaveFailHandledEvent(IMessageContext eventContext, string subscriptionName, Exception e);
        /// <summary>
        /// return event IMessageContext
        /// </summary>
        /// <param name="commandContext"></param>
        /// <param name="eventContexts"></param>
        /// <returns></returns>
        IEnumerable<IMessageContext> SaveCommand(IMessageContext commandContext, IEnumerable<IEvent> eventContexts);
        void SaveFailedCommand(IMessageContext commandContext);
       // void SaveUnSentCommands(IEnumerable<IMessageContext> commandContexts);
        void RemoveSentCommand(string commandId);
        void RemovePublishedEvent(string eventId);
        IEnumerable<IMessageContext> GetAllUnSentCommands();
        IEnumerable<IMessageContext> GetAllUnPublishedEvents();
    }
}
