using System;
using System.Collections.Generic;
using IFramework.Message.Impl;

namespace IFramework.Message
{
    public interface IMessageStore : IDisposable
    {
        bool InMemoryStore { get;}

        CommandHandledInfo GetCommandHandledInfo(string commandId);
        bool HasEventHandled(string eventId, string subscriptionName);

        void HandleEvent(IMessageContext eventContext,
                         string subscriptionName,
                         IEnumerable<IMessageContext> commandContexts,
                         IEnumerable<IMessageContext> messageContexts);

        void SaveFailHandledEvent(IMessageContext eventContext,
                                  string subscriptionName,
                                  Exception e,
                                  params IMessageContext[] messageContexts);

        /// <summary>
        ///     return event IMessageContext
        /// </summary>
        /// <param name="commandContext"></param>
        /// <param name="result"></param>
        /// <param name="eventContexts"></param>
        void SaveCommand(IMessageContext commandContext, object result = null, params IMessageContext[] eventContexts);

        void SaveFailedCommand(IMessageContext commandContext,
                               Exception ex = null,
                               params IMessageContext[] eventContexts);

        IEnumerable<IMessageContext> GetAllUnSentCommands(
            Func<string, IMessage, string, string, string, SagaInfo, string, IMessageContext> wrapMessage);

        IEnumerable<IMessageContext> GetAllUnPublishedEvents(
            Func<string, IMessage, string, string, string, SagaInfo, string, IMessageContext> wrapMessage);

        void Rollback();
    }
}