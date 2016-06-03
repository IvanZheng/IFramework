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
        public async static Task<object> Execute(this ICommandBus commandBus, ICommand command, bool needReply = true)
        {
            var messageResponse = await commandBus.SendAsync(command).ConfigureAwait(false);
            return await messageResponse.Reply.ConfigureAwait(false);
        }

        public async static Task<object> Execute(this ICommandBus commandBus, ICommand command, TimeSpan timeout, bool needReply = true)
        {
            return await commandBus.Execute(command, needReply).Timeout(timeout).ConfigureAwait(false);
        }

        public async static Task<TResult> Send<TResult>(this ICommandBus commandBus, ICommand command, bool needReply = true)
        {
            var messageResponse = await commandBus.SendAsync(command).ConfigureAwait(false);
            return await messageResponse.ReadAsAsync<TResult>().ConfigureAwait(false);
        }


        public async static Task<TResult> Send<TResult>(this ICommandBus commandBus, ICommand command, TimeSpan timeout, bool needReply = true)
        {
            return await commandBus.Send<TResult>(command, needReply).Timeout(timeout).ConfigureAwait(false);
        }


    }
}
