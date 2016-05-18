using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IFramework.Message;

namespace IFramework.Command.Impl
{
    public class MockCommandBus : ICommandBus
    {
       
        public void Send(IEnumerable<IMessageContext> commandContexts)
        {
            
        }

        public Task Send(ICommand command)
        {
            return null;
        }

        public Task Send(ICommand command, CancellationToken cancellationToken)
        {
            return null;
        }

        public Task<TResult> Send<TResult>(ICommand command)
        {
            return null;
        }

        public Task<TResult> Send<TResult>(ICommand command, CancellationToken cancellationToken)
        {
            return null;
        }

        public void Start()
        {
           
        }

        public void Stop()
        {
           
        }

        public IMessageContext WrapCommand(ICommand command, bool needReply = true)
        {
            throw new NotImplementedException();
        }
    }
}
