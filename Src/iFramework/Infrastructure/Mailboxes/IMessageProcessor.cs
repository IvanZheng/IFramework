using System;
using System.Threading.Tasks;
using IFramework.Message;

namespace IFramework.Infrastructure.Mailboxes
{
    public interface IMessageProcessor<TMessage>
        where TMessage : class
    {
        void Start();
        void Stop();
        void Process(IMessageContext messageContext, Func<IMessageContext, Task> process);
    }
}