using System.Threading;
using System.Threading.Tasks;

namespace IFramework.Message.Impl
{
    public class MockMessagePublisher : IMessagePublisher
    {

        public Task<MessageResponse[]> SendAsync(CancellationToken cancellationToken, params MessageState[] messageStates)
        {
            return null;
        }

        public Task<MessageResponse[]> SendAsync(CancellationToken cancellationToken, params IMessage[] events)
        {
            return null;
        }

        public void Start() { }

        public void Stop() { }
    }
}