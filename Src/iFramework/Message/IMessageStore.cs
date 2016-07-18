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
        void HandleEvent(IMessageContext eventContext, string subscriptionName, 
                       IEnumerable<IMessageContext> commandContexts,
                       IEnumerable<IMessageContext> messageContexts);
        void SaveEvent(IMessageContext eventContext);
        void SaveFailHandledEvent(IMessageContext eventContext, string subscriptionName, Exception e, params IMessageContext[] messageContexts);
        /// <summary>
        /// return event IMessageContext
        /// </summary>
        /// <param name="commandContext"></param>
        /// <param name="eventContexts"></param>
        void SaveCommand(IMessageContext commandContext, params IMessageContext[] eventContexts);
        void SaveFailedCommand(IMessageContext commandContext, Exception ex = null, params IMessageContext[] eventContexts);
        void RemoveSentCommand(string commandId);
        void RemovePublishedEvent(string eventId);
        IEnumerable<IMessageContext> GetAllUnSentCommands(Func<string, object, string, string, IMessageContext> wrapMessage);
        IEnumerable<IMessageContext> GetAllUnPublishedEvents(Func<string, object, string, string, IMessageContext> wrapMessage);
        void Rollback();
    }
}
