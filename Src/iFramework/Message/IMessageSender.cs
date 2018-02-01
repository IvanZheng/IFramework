using System.Threading;
using System.Threading.Tasks;

namespace IFramework.Message
{
    public interface IMessageSender
    {
        void Start();
        void Stop();
        Task<MessageResponse[]> SendAsync(CancellationToken cancellationToken, params IMessage[] events);
        Task<MessageResponse[]> SendAsync(CancellationToken cancellationToken, params MessageState[] messageStates);
    }
}