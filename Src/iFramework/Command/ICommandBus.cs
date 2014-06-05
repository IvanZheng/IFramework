using IFramework.Bus;
using IFramework.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IFramework.Command
{
    public interface ICommandBus
    {
        void Start();
        void Stop();
        Task Send(ICommand command, CancellationToken cancellationToken);
        Task Send(ICommand command);
        Task<TResult> Send<TResult>(ICommand command);
        Task<TResult> Send<TResult>(ICommand command, CancellationToken cancellationToken);
    }
}
