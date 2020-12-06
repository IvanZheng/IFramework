using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IFramework.Message.Impl;

namespace IFramework.Message
{
    public interface IMessageStore : IDisposable
    {
        bool InMemoryStore { get;}

        Task<CommandHandledInfo> GetCommandHandledInfoAsync(string commandId);

        Task<bool> HasEventHandledAsync(string eventId, string subscriptionName);

        Task HandleEventAsync(IMessageContext eventContext,
                         string subscriptionName,
                         IEnumerable<IMessageContext> commandContexts,
                         IEnumerable<IMessageContext> messageContexts);

        Task SaveFailHandledEventAsync(IMessageContext eventContext,
                                  string subscriptionName,
                                  Exception e,
                                  params IMessageContext[] messageContexts);

        Task SaveCommandAsync(IMessageContext commandContext, object result = null, params IMessageContext[] eventContexts);

        Task SaveFailedCommandAsync(IMessageContext commandContext,
                               Exception ex = null,
                               params IMessageContext[] eventContexts);

        IEnumerable<IMessageContext> GetAllUnSentCommands(
            Func<string, IMessage, string, string, string, SagaInfo, string, IMessageContext> wrapMessage);

        IEnumerable<IMessageContext> GetAllUnPublishedEvents(
            Func<string, IMessage, string, string, string, SagaInfo, string, IMessageContext> wrapMessage);

        void Rollback();

        void ExecuteByStrategy(Action action);
        Task ExecuteByStrategyAsync(Func<CancellationToken, Task> task, CancellationToken cancellationToken = default);

        void ExecuteInTransactionAsync(Action action);
        Task ExecuteInTransactionAsync(Func<Task> task, CancellationToken cancellationToken = default);

    }
}