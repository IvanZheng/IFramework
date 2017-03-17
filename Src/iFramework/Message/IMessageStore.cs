using IFramework.Message.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.Message
{
    public interface IMessageStore : IDisposable
    {
        CommandHandledInfo GetCommandHandledInfo(string commandId);
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
        void SaveCommand(IMessageContext commandContext, object result = null, params IMessageContext[] eventContexts);
        void SaveFailedCommand(IMessageContext commandContext, Exception ex = null, params IMessageContext[] eventContexts);
        void RemoveSentCommand(string commandId);
        void RemovePublishedEvent(string eventId);
        IEnumerable<IMessageContext> GetAllUnSentCommands(Func<string, IMessage, string, string, string, SagaInfo, string, IMessageContext> wrapMessage);
        IEnumerable<IMessageContext> GetAllUnPublishedEvents(Func<string, IMessage, string, string, string, SagaInfo, string, IMessageContext> wrapMessage);
        void Rollback();
    }
}
