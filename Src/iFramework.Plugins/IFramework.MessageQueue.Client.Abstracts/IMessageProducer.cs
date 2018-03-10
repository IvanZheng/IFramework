using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IFramework.Message;

namespace IFramework.MessageQueue.Client.Abstracts
{
    public interface IMessageProducer
    {
        void Stop();
        Task SendAsync(IMessageContext messageContext, CancellationToken cancellationToken);
    }
}
