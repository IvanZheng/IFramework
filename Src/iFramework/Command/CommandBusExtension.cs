using IFramework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.Command
{
    public static class CommandBusExtension
    {
        public async static Task<object> ExecuteAsync(this ICommandBus commandBus, ICommand command)
        {
            var messageResponse = await commandBus.SendAsync(command, true).ConfigureAwait(false);
            return await messageResponse.Reply.ConfigureAwait(false);
        }

        public async static Task<object> ExecuteAsync(this ICommandBus commandBus, ICommand command, TimeSpan timeout)
        {
            return await commandBus.ExecuteAsync(command).Timeout(timeout).ConfigureAwait(false);
        }

        public async static Task<TResult> ExecuteAsync<TResult>(this ICommandBus commandBus, ICommand command)
        {
            var messageResponse = await commandBus.SendAsync(command, true)
                                                 .ConfigureAwait(false);
            return await messageResponse.ReadAsAsync<TResult>()
                                        .ConfigureAwait(false);
        }

        public async static Task<TResult> ExecuteAsync<TResult>(this ICommandBus commandBus, ICommand command, TimeSpan timeout)
        {
            return await commandBus.ExecuteAsync<TResult>(command)
                                   .Timeout(timeout)
                                   .ConfigureAwait(false);
        }
    }
}
