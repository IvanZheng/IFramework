using System;
using System.Threading.Tasks;
using IFramework.Infrastructure;

namespace IFramework.Command
{
    public static class CommandBusExtension
    {
        public static async Task<object> ExecuteSaga(this ICommandBus commandBus, ICommand command,
            string sagaId = null)
        {
            var messageResponse = await commandBus.StartSaga(command, sagaId).ConfigureAwait(false);
            return await messageResponse.Reply.ConfigureAwait(false);
        }

        public static async Task<object> ExecuteSaga(this ICommandBus commandBus, ICommand command, TimeSpan timeout,
            string sagaId = null)
        {
            return await commandBus.ExecuteSaga(command, sagaId).Timeout(timeout).ConfigureAwait(false);
        }

        public static async Task<TResult> ExecuteSaga<TResult>(this ICommandBus commandBus, ICommand command,
            string sagaId = null)
        {
            var messageResponse = await commandBus.StartSaga(command, sagaId)
                .ConfigureAwait(false);
            return await messageResponse.ReadAsAsync<TResult>()
                .ConfigureAwait(false);
        }

        public static async Task<TResult> ExecuteSaga<TResult>(this ICommandBus commandBus, ICommand command,
            TimeSpan timeout, string sagaId = null)
        {
            return await commandBus.ExecuteSaga<TResult>(command, sagaId)
                .Timeout(timeout)
                .ConfigureAwait(false);
        }

        public static async Task<object> ExecuteAsync(this ICommandBus commandBus, ICommand command)
        {
            var messageResponse = await commandBus.SendAsync(command, true).ConfigureAwait(false);
            return await messageResponse.Reply.ConfigureAwait(false);
        }

        public static async Task<object> ExecuteAsync(this ICommandBus commandBus, ICommand command, TimeSpan timeout)
        {
            return await commandBus.ExecuteAsync(command).Timeout(timeout).ConfigureAwait(false);
        }

        public static async Task<TResult> ExecuteAsync<TResult>(this ICommandBus commandBus, ICommand command)
        {
            var messageResponse = await commandBus.SendAsync(command, true)
                .ConfigureAwait(false);
            return await messageResponse.ReadAsAsync<TResult>()
                .ConfigureAwait(false);
        }

        public static async Task<TResult> ExecuteAsync<TResult>(this ICommandBus commandBus, ICommand command,
            TimeSpan timeout)
        {
            return await commandBus.ExecuteAsync<TResult>(command)
                .Timeout(timeout)
                .ConfigureAwait(false);
        }
    }
}