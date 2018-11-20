using System.Threading.Tasks;

namespace IFramework.Message
{
    public interface IMessageAsyncHandler
    {
        Task Handle(object message);
    }

    public interface IMessageAsyncHandler<in TMessage>
    {
        Task Handle(TMessage message);
    }
}