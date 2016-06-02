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
        Task<MessageResponse> SendAsync(ICommand command, bool needReply = true);
        Task<MessageResponse> SendAsync(ICommand command, TimeSpan timeout, bool needReply = true);
        Task<MessageResponse> SendAsync(ICommand command, CancellationToken sendCancellationToken, TimeSpan sendTimeout, CancellationToken replyCancellationToken, bool needReply = true);
        void SendMessageStates(IEnumerable<MessageState> messageStates);
        IMessageContext WrapCommand(ICommand command, bool needReply = true);
    }
}
