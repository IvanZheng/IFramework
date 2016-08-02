using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IFramework.Message
{
    public interface IMessageSender
    {
        void Start();
        void Stop();
        Task<MessageResponse[]> SendAsync(CancellationToken sendCancellationToken, params IMessage[] events);
        Task<MessageResponse[]> SendAsync(params IMessage[] events);
        Task<MessageResponse[]> SendAsync(params MessageState[] messageStates);
    }
}
