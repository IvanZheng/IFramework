using System.Threading;
using System.Threading.Tasks;

namespace IFramework.Message
{
    public interface IMessageAsyncHandler<in TMessage>
    {
        Task Handle(TMessage message, CancellationToken cancellationToken = default);
    }
}