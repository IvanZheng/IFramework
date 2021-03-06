using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IFramework.Message.Impl
{
    public class MockMessageStore : IMessageStore
    {
        public Task<bool> HasEventHandledAsync(string eventId, string subscriptionName)
        {
            return Task.FromResult(false);
        }

        public void RemoveSentCommand(string commandId) { }

        public void RemovePublishedEvent(string eventId) { }


        public void Dispose() { }


        public Task HandleEventAsync(IMessageContext eventContext,
                                     string subscriptionName,
                                     IEnumerable<IMessageContext> commandContexts,
                                     IEnumerable<IMessageContext> messageContexts)
        {
            return Task.CompletedTask;
        }
        
        public IEnumerable<IMessageContext> GetAllUnSentCommands(
            Func<string, IMessage, string, string, string, SagaInfo, string, IMessageContext> wrapMessage)
        {
            return null;
        }

        public IEnumerable<IMessageContext> GetAllUnPublishedEvents(
            Func<string, IMessage, string, string, string, SagaInfo, string, IMessageContext> wrapMessage)
        {
            return null;
        }

        public void Rollback() { }
        public void ExecuteByStrategy(Action action)
        {
            action?.Invoke();
        }

        public Task ExecuteByStrategyAsync(Func<CancellationToken, Task> task, CancellationToken cancellationToken = default)
        {
            return task?.Invoke(cancellationToken);
        }

        public void ExecuteInTransactionAsync(Action action)
        {
            action?.Invoke();
        }


        public Task ExecuteInTransactionAsync(Func<Task> task, CancellationToken cancellationToken)
        {
            return task?.Invoke();
        }


        public Task SaveFailedCommandAsync(IMessageContext commandContext,
                                           Exception ex = null,
                                           params IMessageContext[] eventContexts)
        {
            return Task.CompletedTask;
        }

        public Task SaveFailHandledEventAsync(IMessageContext eventContext,
                                              string subscriptionName,
                                              Exception e,
                                              params IMessageContext[] messageContexts)
        {
            return Task.CompletedTask;
        }
        
        public bool InMemoryStore => true;

        public Task<CommandHandledInfo> GetCommandHandledInfoAsync(string commandId)
        {
            return Task.FromResult((CommandHandledInfo)null);
        }

        public Task SaveCommandAsync(IMessageContext commandContext,
                                     object result = null,
                                     params IMessageContext[] eventContexts)
        {
            return Task.CompletedTask;
        }
    }
}